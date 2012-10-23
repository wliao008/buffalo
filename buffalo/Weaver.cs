using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reflection = System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Specialized;

namespace Buffalo
{
    internal class Weaver
    {
        public Weaver(string assemblyPath)
        {
            ///TODO: Maybe just don't do anything if file not found
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException();

            AssemblyPath = assemblyPath;
            this.Init();
        }

        static internal string AssemblyPath { get; set; }

        static internal List<Aspect> Aspects { get; set; }

        static internal Dictionary<string, Type> UnderlyingAspectTypes { get; set; }

        internal Dictionary<MethodDefinition, List<Aspect>> EligibleMethods { get; set; }

        internal List<TypeDefinition> TypeDefinitions { get; set; }

        internal AssemblyDefinition AssemblyDefinition { get; set; }

        internal StringCollection NewMethodNames { get; set; }

        internal void Inject(string outPath)
        {
            //apply the around aspect if necessary
            var aroundAspectExist = this.EligibleMethods.Values.Any(x => x.Any(y => y.Type.BaseType == typeof(MethodAroundAspect)));
            if (aroundAspectExist)
            {
                this.InjectAroundAspect();
            }

            //apply the boundary aspect if necessary
            var boundaryAspectExist = this.EligibleMethods.Values.Any(x => x.Any(y => y.Type.BaseType == typeof(MethodBoundaryAspect)));
            if (boundaryAspectExist)
            {
                this.InjectBoundaryAspect();
            }

            //write out the modified assembly
            this.AssemblyDefinition.Write(outPath);
            Console.WriteLine("DONE");
        }

        private void Init()
        {
            //initialize the variables
            Aspects = new List<Aspect>();
            NewMethodNames = new StringCollection();
            this.TypeDefinitions = new List<TypeDefinition>();
            this.EligibleMethods = new Dictionary<MethodDefinition, List<Aspect>>();
            this.AssemblyDefinition = AssemblyDefinition.ReadAssembly(AssemblyPath);
            //populate the type definition first
            foreach (var m in this.AssemblyDefinition.Modules)
                m.Types.ToList().ForEach(x => this.TypeDefinitions.Add(x));
            //extract aspects from the type definitions
            this.TypeDefinitions
                .Where(x => x.BaseType != null 
                    && (x.BaseType.FullName == typeof(MethodBoundaryAspect).FullName 
                    || x.BaseType.FullName == typeof(MethodAroundAspect).FullName))
                .ToList()
                .ForEach(x => Aspects.Add(new Aspect { Name = x.FullName, TypeDefinition = x }));
            //set the original types
            SetUnderlyingAspectTypes();
            Aspects.ForEach(x =>
            {
                x.AssemblyLevelStatus = this.CheckAspectStatus(this.AssemblyDefinition, x);
            });
            //finally, get the eligible methods
            Aspects
                .Where(x => x.AssemblyLevelStatus != Buffalo.Enums.Status.Excluded)
                .ToList()
                .ForEach(x =>
                {
#if DEBUG
                    Console.WriteLine("Aspect {0}: {1}", x.Name, x.AssemblyLevelStatus.ToString());
                    Console.WriteLine("============================================");
#endif
                    this.CheckEligibleMethods(x);
                    Console.WriteLine("");
                });
        }

        private void InjectAroundAspect()
        {
            var once = false;
            var ems = this.EligibleMethods.ToList();
            var eligibleAroundMethods = ems.Where(x => x.Value.All(y => y.Type.BaseType == typeof(MethodAroundAspect)));
            foreach (var d in eligibleAroundMethods)
            {
                var targetMethod = d.Key;
                var aroundAspect = d.Value[0];

                //create a replacement function
                var methodName = string.Format("{0}{1}", targetMethod.Name, DateTime.Now.Ticks);
                var aroundMethod = aroundAspect.TypeDefinition.Methods.SingleOrDefault(x => x.FullName.Contains("Invoke(Buffalo.MethodDetail)"));
                var instProceed = aroundMethod.Body.Instructions.FirstOrDefault(x => x.ToString().Contains("callvirt System.Void Buffalo.MethodDetail::Proceed()"));
                //TypeReference voidref = this.AssemblyDefinition.MainModule.Import(typeof(void));
                MethodDefinition md = new MethodDefinition(methodName, targetMethod.Attributes, targetMethod.ReturnType);

                if (!once)
                {
                    //copy the variables in aspect to the target type, this should happen only once?
                    aroundAspect.TypeDefinition.Fields.ToList()
                        .ForEach(x =>
                        {
                            //var constructorInfo = typeof(Instruction).GetConstructor(Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance, null, new[] { typeof(OpCode), typeof(object) }, null);
                            //var newInstruction = (Instruction)constructorInfo.Invoke(new[] { x.OpCode, instruction.Operand });
                            //var fieldDefinition = newInstruction.Operand as FieldDefinition;

                            //if (newInstruction.Operand is TypeReference)
                            //{
                            //    this.AssemblyDefinition.MainModule.Import(newInstruction.Operand as TypeReference);
                            //}

                            var fd = new FieldDefinition(x.Name, x.Attributes, x.FieldType);
                            
                            targetMethod.DeclaringType.Fields.Add(fd);
                        });

                    once = true;
                }

                md.Body.SimplifyMacros();

                targetMethod.Parameters.ToList().ForEach(x => md.Parameters.Add(new ParameterDefinition(x.ParameterType)));
                aroundMethod.Body.Variables.ToList().ForEach(x => md.Body.Variables.Add(new VariableDefinition(x.VariableType)));
                
                //aroundMethod.Body.Instructions.ToList().ForEach(x => md.Body.Instructions.Add(Instruction.Create(x.OpCode, x.Operand)));
                aroundMethod.Body.Instructions.ToList().ForEach(instruction =>
                {
                    var constructorInfo = typeof(Instruction).GetConstructor(Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance, null, new[] { typeof(OpCode), typeof(object) }, null);
                    var newInstruction = (Instruction)constructorInfo.Invoke(new[] { instruction.OpCode, instruction.Operand});
                    var fieldDefinition = newInstruction.Operand as FieldDefinition;
                    if (fieldDefinition != null)
                    {
                        this.AssemblyDefinition.MainModule.Import(fieldDefinition.FieldType);
                        newInstruction.Operand = targetMethod.DeclaringType.Fields.First(x => x.Name == fieldDefinition.Name);
                    }

                    if (newInstruction.Operand is MethodReference)
                    {
                        //Try really hard to import type
                        var methodReference = (MethodReference)newInstruction.Operand;

                        this.AssemblyDefinition.MainModule.Import(methodReference.MethodReturnType.ReturnType);
                        this.AssemblyDefinition.MainModule.Import(methodReference.DeclaringType);
                        this.AssemblyDefinition.MainModule.Import(methodReference);
                    }
                    if (newInstruction.Operand is TypeReference)
                    {
                        this.AssemblyDefinition.MainModule.Import(newInstruction.Operand as TypeReference);
                    }
                    md.Body.Instructions.Add(newInstruction);
                });
                NewMethodNames.Add(methodName);
                targetMethod.DeclaringType.Methods.Add(md);

                var invokeMethod = aroundAspect.TypeDefinition.Methods.SingleOrDefault(x => x.FullName.Contains("Invoke(Buffalo.MethodDetail)"));
                int i = 0;
                bool found = false;
                for (i = 0; i < invokeMethod.Body.Instructions.Count; ++i)
                {
                    if (invokeMethod.Body.Instructions[i].ToString()
                        .Contains("callvirt System.Void Buffalo.MethodDetail::Proceed()"))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    var inst = md.Body.Instructions[i];
                    var methodRef = this.FindMethodReference(targetMethod, aroundAspect, Enums.BoundaryType.Invoke);
                    //var il = invokeMethod.Body.GetILProcessor();
                    //var instCall = Instruction.Create(OpCodes.Call, targetMethod);
                    //md.Body.Instructions[i].Operand = targetMethod;
                    md.Body.Instructions[i - 1] = Instruction.Create(OpCodes.Ldarg_0);
                    md.Body.Instructions[i] = Instruction.Create(OpCodes.Call, targetMethod);
                    //md.Body.OptimizeMacros();
                }

                //finally, all calls to the original methods should be changed to the newly
                //generated method
                foreach (var type in this.AssemblyDefinition.MainModule.Types)
                {
                    foreach (var m in type.Methods.Where(x => !NewMethodNames.Contains(x.Name)))
                    {
                        for (int j = 0; j < m.Body.Instructions.Count; ++j)
                        {
                            if (m.Body.Instructions[j].ToString().Contains(targetMethod.FullName))
                            {
                                //m.Body.Instructions[j].OpCode = OpCodes.Call;
                                m.Body.Instructions[j].Operand = md;
                            }
                        }
                        
                        m.Body.OptimizeMacros();
                    }
                }
            }
        }

        private void InjectBoundaryAspect()
        {
            //TcfMarker marker = new TcfMarker();
            var ems = this.EligibleMethods.ToList();
            var eligibleBoundaryMethods = ems.Where(x => x.Value.All(y => y.Type.BaseType == typeof(MethodBoundaryAspect)));

            foreach (var d in eligibleBoundaryMethods)
            {
                var method = d.Key;
                var il = method.Body.GetILProcessor();
                var ret = il.Create(OpCodes.Ret);
                var pop = il.Create(OpCodes.Pop);
                var leave = il.Create(OpCodes.Leave, ret);
                var count = method.Body.Instructions.Count;
                method.Body.Instructions[count - 1] = ret;
                var beforeMarker = this._DoBefore(d);
                var successMarker = this._DoSuccess(d, leave);
                var exceptionMarker = this._DoException(d, leave);
                il.InsertAfter(method.Body.Instructions.Last().Previous, leave);

                var marker = new TcfMarker();
                marker.TryInnerStart = method.Body.Instructions[beforeMarker.BeginIndex];
                marker.TryInnerEnd = method.Body.Instructions[exceptionMarker.BeginIndex];
                marker.CatchInnerStart = method.Body.Instructions[exceptionMarker.BeginIndex];
                marker.CatchInnerEnd = method.Body.Instructions[exceptionMarker.EndIndex];
                this._DoCatch(marker, d.Key);
                //this._DoFinally(marker, d.Key, ret);
            }
        }

        public BeginEndMarker _DoBefore(KeyValuePair<MethodDefinition, List<Aspect>> kv)
        {
            var method = kv.Key;
            var aspects = kv.Value;
            var il = method.Body.GetILProcessor();
            List<Instruction> beforeInstructions = new List<Instruction>();
            for (int i = 0; i < aspects.Count; ++i)
            {
                var before = this.FindMethodReference(method, aspects[i], Buffalo.Enums.BoundaryType.Before);
                if (before != null)
                {
                    beforeInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                    beforeInstructions.Add(Instruction.Create(OpCodes.Call, before));
                }
            }

            int idx = 0;
            beforeInstructions.ForEach(x => method.Body.Instructions.Insert(idx++, x));
            var marker = new BeginEndMarker
            {
                BeginIndex = 0,
                EndIndex = method.Body.Instructions.Count
            };
            return marker;
        }

        public BeginEndMarker _DoSuccess(KeyValuePair<MethodDefinition, List<Aspect>> kv, Instruction ret)
        {
            var method = kv.Key;
            var aspects = kv.Value;
            var il = method.Body.GetILProcessor();
            List<Instruction> successInstructions = new List<Instruction>();
            for (int i = 0, j = 0; i <= aspects.Count; i += 2, ++j)
            {
                var success = this.FindMethodReference(method, aspects[j], Buffalo.Enums.BoundaryType.Success);
                if (success != null)
                {
                    //writeSuccess = il.Create(OpCodes.Call, success);
                    //method.Body.Instructions.Insert(idx + i + 1, Instruction.Create(OpCodes.Ldarg_0));
                    //method.Body.Instructions.Insert(idx + i + 2, writeSuccess);
                    successInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                    successInstructions.Add(Instruction.Create(OpCodes.Call, success));
                }
            }

            successInstructions.Add(Instruction.Create(OpCodes.Leave, ret));

            /* the success instructions are inserted at the end of the method body,
             * but since the last instruction is always the ret instruction, we need to
             * insert them right before ret.
             */
            int idx = method.Body.Instructions.Count() - 1;
            var markerBeginIdx = idx + 1;
            successInstructions.ForEach(x => method.Body.Instructions.Insert(idx++, x));
            var marker = new BeginEndMarker
            {
                BeginIndex = 0,
                EndIndex = method.Body.Instructions.Count
            };
            return marker;
        }

        public BeginEndMarker _DoException(KeyValuePair<MethodDefinition, List<Aspect>> kv, Instruction ret)
        {
            var method = kv.Key;
            var aspects = kv.Value;
            var il = method.Body.GetILProcessor();
            List<Instruction> exceptionInstructions = new List<Instruction>();
            for (int i = 0, j = 0; i <= aspects.Count; i += 2, ++j)
            {
                var exception = this.FindMethodReference(method, aspects[j], Buffalo.Enums.BoundaryType.Exception);
                if (exception != null)
                {
                    //writeSuccess = il.Create(OpCodes.Call, success);
                    //method.Body.Instructions.Insert(idx + i + 1, Instruction.Create(OpCodes.Ldarg_0));
                    //method.Body.Instructions.Insert(idx + i + 2, writeSuccess);
                    exceptionInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                    exceptionInstructions.Add(Instruction.Create(OpCodes.Call, exception));
                }
            }

            exceptionInstructions.Add(Instruction.Create(OpCodes.Leave, ret));

            /* the success instructions are inserted at the end of the method body,
             * but since the last instruction is always the ret instruction, we need to
             * insert them right before ret.
             */
            int idx = method.Body.Instructions.Count() - 1;
            var markerBeginIdx = idx;
            exceptionInstructions.ForEach(x => method.Body.Instructions.Insert(idx++, x));

            var marker = new BeginEndMarker
            {
                BeginIndex = markerBeginIdx,
                EndIndex = method.Body.Instructions.Count - 1
            };
            return marker;
        }

        public void _DoCatch(TcfMarker marker, MethodDefinition method)
        {
            var catchHandler = new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                TryStart = marker.TryInnerStart,
                TryEnd = marker.TryInnerEnd,
                HandlerStart = marker.CatchInnerStart,
                HandlerEnd = marker.CatchInnerEnd,// endfinally,
                CatchType = this.AssemblyDefinition.MainModule.Import(typeof(Exception)),
            };
            method.Body.ExceptionHandlers.Add(catchHandler);
        }

        public void _DoFinally(TcfMarker marker, MethodDefinition method, Instruction ret)
        {
            var finallyHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = marker.TryStart,
                TryEnd = marker.TryEnd,
                HandlerStart = marker.HandlerStart,
                HandlerEnd = marker.HandlerEnd,// endfinally,
                CatchType = null,
            };
            method.Body.ExceptionHandlers.Add(finallyHandler);
        }

        private void InjectBoundaryAspect_OLD()
        {
            TcfMarker marker = new TcfMarker();
            var ems = this.EligibleMethods.ToList();
            var eligibleBoundaryMethods = ems.Where(x => x.Value.All(y => y.Type.BaseType == typeof(MethodBoundaryAspect)));
            foreach (var d in eligibleBoundaryMethods)
            {
                var method = d.Key;
                var aspects = d.Value;
                var count = method.Body.Instructions.Count;
                var il = method.Body.GetILProcessor();
                Instruction writeSuccess = null;
                List<Instruction> beforeInstructions = new List<Instruction>();
                List<Instruction> afterInstructions = new List<Instruction>();
                List<Instruction> successInstructions = new List<Instruction>();
                List<Instruction> exceptionInstructions = new List<Instruction>();
                
                var ret = il.Create(OpCodes.Ret);
                var pop = il.Create(OpCodes.Pop);
                var leave = il.Create(OpCodes.Leave, ret);
                method.Body.Instructions[count - 1] = Instruction.Create(OpCodes.Leave_S, ret);

                //Before()
                for (int i = 0; i < aspects.Count; ++i)
                {
                    var before = this.FindMethodReference(method, aspects[i], Buffalo.Enums.BoundaryType.Before);
                    if (before != null)
                    {
                        /*
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        var varType = this.AssemblyDefinition.MainModule.Import(typeof(MethodDetail));
                        var varDef = new VariableDefinition("mdb" + i, varType);
                        beforeInstructions.Add(Instruction.Create(OpCodes.Stloc, varDef));
                        method.Body.Variables.Add(varDef);
                        //beforeInstructions.Add(Instruction.Create(OpCodes.Nop));
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldloc, varDef));
                        var constructorInfo = typeof(MethodDetail).GetConstructor(new Type[] { });
                        MethodReference myClassConstructor = this.AssemblyDefinition.MainModule.Import(constructorInfo);
                        beforeInstructions.Add(Instruction.Create(OpCodes.Newobj, myClassConstructor));
                        beforeInstructions.Add(Instruction.Create(OpCodes.Stloc_0));
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldloc_0));
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldstr, method.Name));
                        var detailSetName = varType.Resolve().Methods.FirstOrDefault(x => x.Name.Equals("setName"));
                        var detailSetNameRef = this.AssemblyDefinition.MainModule.Import(detailSetName, before);
                        beforeInstructions.Add(Instruction.Create(OpCodes.Callvirt, detailSetNameRef));
                        */
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        //beforeInstructions.Add(Instruction.Create(OpCodes.Ldloc_0));
                        beforeInstructions.Add(Instruction.Create(OpCodes.Call, before));
                    }
                }

                int idx = 0;
                beforeInstructions.ForEach(x => method.Body.Instructions.Insert(idx++, x));

                var in1 = il.Create(OpCodes.Stloc_2);
                var in2 = il.Create(OpCodes.Nop);
                var in3 = il.Create(OpCodes.Ldarg_0);
                var in4 = il.Create(OpCodes.Ldloc_2);
                var in5 = il.Create(OpCodes.Nop);
                var in6 = il.Create(OpCodes.Nop);

                idx = method.Body.Instructions.Count - 1;
                method.Body.Instructions.Insert(idx, Instruction.Create(OpCodes.Nop));
                for (int i = 0, j = 0; i <= aspects.Count; i += 2, ++j)
                {
                    //any Success()?
                    var success = this.FindMethodReference(method, aspects[j], Buffalo.Enums.BoundaryType.Success);
                    if (success != null)
                    {
                        writeSuccess = il.Create(OpCodes.Call, success);
                        method.Body.Instructions.Insert(idx + i + 1, Instruction.Create(OpCodes.Ldarg_0));
                        method.Body.Instructions.Insert(idx + i + 2, writeSuccess);
                    }
                }
                
                idx = method.Body.Instructions.Count - 1;
                method.Body.Instructions.Insert(idx, Instruction.Create(OpCodes.Nop));
                for (int i = 0, j = 0; i <= aspects.Count; i += 2, ++j)
                {
                    //any Exception()?
                    var exception = this.FindMethodReference(method, aspects[j], Buffalo.Enums.BoundaryType.Exception);
                    if (exception != null)
                    {
                        var inst1 = il.Create(OpCodes.Ldarg_0);
                        var inst2 = il.Create(OpCodes.Call, exception);
                        exceptionInstructions.Add(inst1);
                        exceptionInstructions.Add(inst2);

                        /*
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        var varType = this.AssemblyDefinition.MainModule.Import(typeof(MethodDetail));
                        var varDef = new VariableDefinition("mdb" + i, varType);
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Stloc, varDef));
                        method.Body.Variables.Add(varDef);
                        //beforeInstructions.Add(Instruction.Create(OpCodes.Nop));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc, varDef));
                        var constructorInfo = typeof(MethodDetail).GetConstructor(new Type[] { });
                        MethodReference myClassConstructor = this.AssemblyDefinition.MainModule.Import(constructorInfo);
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Newobj, myClassConstructor));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Stloc_0));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc_0));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldstr, method.Name));
                        var detailSetName = varType.Resolve().Methods.FirstOrDefault(x => x.Name.Equals("setName"));
                        var detailSetNameRef = this.AssemblyDefinition.MainModule.Import(detailSetName, exception);
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Callvirt, detailSetNameRef));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc_0));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Call, exception));
                        */
                    }
                }
                method.Body.OptimizeMacros();

                //the beginning of the catch.. block actually marks the end of the try.. block
                ///TODO: This is a bug, it should be the first writeException, not the last one
                ///TODO: This cause runtime exception when no Exception() has been overriden.
                if (exceptionInstructions.Count == 0)
                {
                    //a dummy instruction
                    exceptionInstructions.Add(il.Create(OpCodes.Nop));
                }

                exceptionInstructions.ForEach(x =>
                {
                    il.InsertAfter(method.Body.Instructions.Last(), x);
                });

                marker.TryEnd = exceptionInstructions[0];
                var lastExceptionInst = exceptionInstructions.Last();
                il.InsertAfter(lastExceptionInst, leave);

                var endfinally = il.Create(OpCodes.Endfinally);

                idx = method.Body.Instructions.Count - 1;
                int finallyStartIdx = idx;
                for (int i = 0; i < aspects.Count; ++i)
                {
                    //any After()?
                    var after = this.FindMethodReference(method, aspects[i], Buffalo.Enums.BoundaryType.After);
                    if (after != null)
                    {
                        /*
                        afterInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        var varType = this.AssemblyDefinition.MainModule.Import(typeof(MethodDetail));
                        var varDef = new VariableDefinition("mda" + i, varType);
                        afterInstructions.Add(Instruction.Create(OpCodes.Stloc, varDef));
                        method.Body.Variables.Add(varDef);
                        //beforeInstructions.Add(Instruction.Create(OpCodes.Nop));
                        afterInstructions.Add(Instruction.Create(OpCodes.Ldloc, varDef));
                        var constructorInfo = typeof(MethodDetail).GetConstructor(new Type[] { });
                        MethodReference myClassConstructor = this.AssemblyDefinition.MainModule.Import(constructorInfo);
                        afterInstructions.Add(Instruction.Create(OpCodes.Newobj, myClassConstructor));
                        afterInstructions.Add(Instruction.Create(OpCodes.Stloc_0));
                        afterInstructions.Add(Instruction.Create(OpCodes.Ldloc_0));
                        afterInstructions.Add(Instruction.Create(OpCodes.Ldstr, method.Name));
                        var detailSetName = varType.Resolve().Methods.FirstOrDefault(x => x.Name.Equals("setName"));
                        var detailSetNameRef = this.AssemblyDefinition.MainModule.Import(detailSetName, after);
                        afterInstructions.Add(Instruction.Create(OpCodes.Callvirt, detailSetNameRef));
                        */
                        afterInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        afterInstructions.Add(Instruction.Create(OpCodes.Ldloc_0));
                        afterInstructions.Add(Instruction.Create(OpCodes.Call, after));
                    }
                }
                afterInstructions.Add(endfinally);
                afterInstructions.ForEach(x => il.InsertAfter(method.Body.Instructions.Last(), x));
                il.InsertAfter(endfinally, ret);
                method.Body.OptimizeMacros();

                marker.TryStart = method.Body.Instructions.First();
                var catchHandler = new ExceptionHandler(ExceptionHandlerType.Catch)
                {
                    TryStart = marker.TryStart,
                    TryEnd = marker.TryEnd,
                    HandlerStart = exceptionInstructions[0],
                    HandlerEnd = afterInstructions.First(),// endfinally,
                    CatchType = this.AssemblyDefinition.MainModule.Import(typeof(Exception)),
                };
                var finallyHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
                {
                    TryStart = method.Body.Instructions.First(),
                    TryEnd = afterInstructions.First(),
                    HandlerStart = method.Body.Instructions[finallyStartIdx+1],// endfinally,
                    HandlerEnd = ret,
                    CatchType = null,
                };
                method.Body.ExceptionHandlers.Add(catchHandler);
                method.Body.ExceptionHandlers.Add(finallyHandler);
            }
        }

        private MethodReference FindMethodReference(MethodDefinition method, Aspect aspect, Buffalo.Enums.BoundaryType name)
        {
            return aspect
                .TypeDefinition
                .Methods
                .FirstOrDefault(x => x.Name.Equals(name.ToString()));
        }

        private static void SetUnderlyingAspectTypes()
        {
            ///TODO: This block would not be needed if I can get the underlying types
            ///directly from cecil
            AppDomain domain = AppDomain.CreateDomain("domain");
            BoundaryObject boundary = (BoundaryObject)
                domain.CreateInstanceAndUnwrap(
                typeof(BoundaryObject).Assembly.FullName,
                typeof(BoundaryObject).FullName);
            AppDomainArgs ada = new AppDomainArgs();
            ada.AssemblyPath = AssemblyPath;
            ada.Aspects = Aspects;
            ada.UnderlyingAspectTypes = UnderlyingAspectTypes;
            //domain.DoCallBack(new CrossAppDomainDelegate(LoadAssembly));
            BoundaryObject.DoSomething(ada, AssemblyPath);
            domain.DomainUnload += new EventHandler(domain_DomainUnload);
            AppDomain.Unload(domain);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private static void domain_DomainUnload(object sender, EventArgs e)
        {
            Console.WriteLine("Unloading domain...");
        }

        private static void LoadAssembly()
        {
            ///TODO: need to pass vars to and from appdomains: http://stackoverflow.com/a/1250847/150607
            var _assemblyPath = @"C:\Users\wliao\Documents\Visual Studio 2010\Projects\buffalo\client\bin\Debug\client.exe";
            var assembly = System.Reflection.Assembly.LoadFrom(_assemblyPath);
            var types = assembly.GetTypes().ToList();
        }

        private void PrintEligibleMethods()
        {
            foreach (var de in this.EligibleMethods)
            {
                Console.WriteLine(de.Key.FullName);
                foreach (var a in de.Value)
                {
                    Console.WriteLine("\t" + a.Name);
                }
            }
        }

        [Obsolete]
        private void CheckEligibleMethods()
        {
            Aspects.ForEach(x => this.CheckEligibleMethods(x));
        }

        private void CheckEligibleMethods(Aspect aspect)
        {
            foreach (var t in this.TypeDefinitions.Where(x => !x.Name.Equals("<Module>")))
            {
                var status = this.CheckAspectStatus(t, aspect);
#if DEBUG
                Console.WriteLine("\t{0}: {1}", t.Name, status.ToString());
#endif
                if (status == Buffalo.Enums.Status.Excluded)
                    continue;

                var mths = this.GetMethodDefinitions(t, status, aspect);
                mths.ForEach(x =>
                {
                    if (!this.EligibleMethods.ContainsKey(x))
                    {
                        this.EligibleMethods.Add(x, new List<Aspect>() { aspect });
                    }
                    else
                    {
                        var aspects = this.EligibleMethods[x];
                        aspects.Add(aspect);
                    }
                });
            }
        }

        private List<MethodDefinition> GetMethodDefinitions(TypeDefinition typeDef, Buffalo.Enums.Status typeStatus, Aspect aspect)
        {
            if (typeDef.Name.Contains("Test"))
            {
                System.Diagnostics.Debug.WriteLine("Test");
            }

            var list = new List<MethodDefinition>();
            foreach (var method in typeDef.Methods)
            {
                var status = this.CheckAspectStatus(method, aspect);
                //only add methods that are not excluded
                //if (status != Status.Applied && typeStatus != Status.Applied)
                //    continue;

                if (status == Buffalo.Enums.Status.Applied)
                {
                    list.Add(method);
                }
                else
                {
                    if (typeStatus == Buffalo.Enums.Status.Applied && status != Buffalo.Enums.Status.Excluded)
                    {
                        status = Buffalo.Enums.Status.Applied;
                        list.Add(method);
                    }
                }
#if DEBUG
                Console.WriteLine("\t\t{0}: {1}", method.Name, status.ToString());
#endif
            }

            return list;
        }

        /// <summary>
        /// A TypeDefinition and MethodDefinition both implement the
        /// ICustomAttributeProvider interface, so it can be used here
        /// to determined if a method is marked as exclude or not.
        /// </summary>
        private Buffalo.Enums.Status CheckAspectStatus(ICustomAttributeProvider def, Aspect aspect)
        {
            Buffalo.Enums.Status status = aspect.AssemblyLevelStatus;

            bool attrFound = false;
            for (int i = 0; i < def.CustomAttributes.Count; ++i)
            {
                //var t = def.CustomAttributes[i].AttributeType.Resolve();
                if (aspect.Type != null 
                    && (aspect.Type.BaseType == typeof(MethodBoundaryAspect)
                    || aspect.Type.BaseType == typeof(MethodAroundAspect))
                    && def.CustomAttributes[i].AttributeType.FullName.Equals(aspect.Name))
                {
                    attrFound = true;
                    if (def.CustomAttributes[i].Properties.Count == 0)
                    {
                        status = Buffalo.Enums.Status.Applied;
                    }
                    else
                    {
                        var exclude = def.CustomAttributes[i].Properties.First(x => x.Name == "AttributeExclude");
                        if ((bool)exclude.Argument.Value == true)
                        {
                            status = Buffalo.Enums.Status.Excluded;
                            //Console.WriteLine(def.CustomAttributes[i].AttributeType.Name + " removed");
                            def.CustomAttributes.RemoveAt(i);
                        }
                    }
                }
            }

            if (!attrFound && aspect.AssemblyLevelStatus == Buffalo.Enums.Status.Applied)
            {
                //this aspect is applied on the assembly level and
                //as a result the type and method might not have the
                //attributed annotated, this is to programmatically add
                //in the annotation so IL can be generated correctly.
                MethodReference attrCtor = this.AssemblyDefinition.MainModule.Import(
                    aspect.Type.GetConstructor(Type.EmptyTypes));
                //var methodRef = this.AssemblyDefinition.MainModule.Import(aspect.Type);
                
                def.CustomAttributes.Add(new CustomAttribute(attrCtor));
                //Console.WriteLine("Injecting custome attr for: " + def.ToString());
            }

            return status;
        }
    }

    class BoundaryObject : MarshalByRefObject
    {
        public static void DoSomething(AppDomainArgs ada, string path)
        {
            ///TODO: need to pass vars to and from appdomains: http://stackoverflow.com/a/1250847/150607
            var _assemblyPath = path;
            var assembly = System.Reflection.Assembly.LoadFrom(_assemblyPath);
            var types = assembly.GetTypes().ToList();

            foreach (var aspect in ada.Aspects)
            {
                var type = types.FirstOrDefault(x => x.FullName.Equals(aspect.TypeDefinition.FullName));
                if (type != null)
                {
                    aspect.Type = type;
                }
            }
        }
    }

    class AppDomainArgs : MarshalByRefObject
    {
        internal string AssemblyPath { get; set; }

        internal List<Aspect> Aspects { get; set; }

        internal Dictionary<string, Type> UnderlyingAspectTypes { get; set; }
    }
}