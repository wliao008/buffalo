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
            var ems = this.EligibleMethods.ToList();
            var eligibleBoundaryMethods = ems.Where(x => x.Value.All(y => y.Type.BaseType == typeof(MethodBoundaryAspect)));
            foreach (var d in eligibleBoundaryMethods)
            {
                var method = d.Key;
                var aspects = d.Value;
                var il = method.Body.GetILProcessor();
                var voidType = method.ReturnType.FullName.Equals("System.Void");
                var ret = il.Create(OpCodes.Ret);

                var mdInstructions = new List<Instruction>();
                var beforeInstructions = new List<Instruction>();
                var successInstructions = new List<Instruction>();
                var exceptionInstructions = new List<Instruction>();
                var afterInstructions = new List<Instruction>();

                method.Body.SimplifyMacros();
                #region Method detail
                //var mdType = this.AssemblyDefinition.MainModule.Import(typeof(MethodDetail));
                //var mdSetName = mdType.Resolve().Methods.FirstOrDefault(x => x.Name.Equals("setName"));
                //var mdSetException = mdType.Resolve().Methods.FirstOrDefault(x => x.Name.Equals("setException"));
                //var mdCtorInfo = typeof(MethodDetail).GetConstructor(new Type[] { });
                //MethodReference mdCtor = this.AssemblyDefinition.MainModule.Import(mdCtorInfo);
                //mdInstructions.Add(Instruction.Create(OpCodes.Newobj, mdCtor));
                //mdInstructions.Add(Instruction.Create(OpCodes.Stloc_0)); //store methoddetail at index 0
                //mdInstructions.Add(Instruction.Create(OpCodes.Ldloc_0));
                //mdInstructions.Add(Instruction.Create(OpCodes.Ldstr, method.Name));
                //var mdSetNameRef = this.AssemblyDefinition.MainModule.Import(mdSetName, method);
                //var mdSetExceptionRef = this.AssemblyDefinition.MainModule.Import(mdSetException, method);
                //mdInstructions.Add(Instruction.Create(OpCodes.Callvirt, mdSetNameRef));
                int mdIdx = 0;
                //mdInstructions.ForEach(x => method.Body.Instructions.Insert(mdIdx++, x));

                /*
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        var varType = this.AssemblyDefinition.MainModule.Import(typeof(MethodDetail));
                        var varDef = new VariableDefinition("md" + i, varType);
                        beforeInstructions.Add(Instruction.Create(OpCodes.Stloc, varDef));
                        method.Body.Variables.Add(varDef);
                        //beforeInstructions.Add(Instruction.Create(OpCodes.Nop));
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldloc, varDef));
                        var constructorInfo = typeof(MethodDetail).GetConstructor(new Type[] { });
                        MethodReference myClassConstructor = this.AssemblyDefinition.MainModule.Import(constructorInfo);
                        beforeInstructions.Add(Instruction.Create(OpCodes.Newobj, myClassConstructor));
                        beforeInstructions.Add(Instruction.Create(OpCodes.Call, before));
                 */
                #endregion

                #region Before, Success, Exception, After
                for (int i = 0; i < aspects.Count; ++i)
                {
                    var before = this.FindMethodReference(method, aspects[i], Buffalo.Enums.BoundaryType.Before);
                    if (before != null)
                    {
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        //beforeInstructions.Add(Instruction.Create(OpCodes.Ldloc_0));
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldstr, method.Name));
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldstr, method.FullName));
                        beforeInstructions.Add(Instruction.Create(OpCodes.Call, before));
                    }

                    var success = this.FindMethodReference(method, aspects[i], Buffalo.Enums.BoundaryType.Success);
                    if (success != null)
                    {
                        successInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        successInstructions.Add(Instruction.Create(OpCodes.Call, success));
                    }

                    var exception = this.FindMethodReference(method, aspects[i], Buffalo.Enums.BoundaryType.Exception);
                    if (exception != null)
                    {
                        var varExpType = this.AssemblyDefinition.MainModule.Import(typeof(System.Exception));
                        var toString = varExpType.Resolve().Methods.FirstOrDefault(x => x.Name.Equals("ToString"));
                        var toStringRef = this.AssemblyDefinition.MainModule.Import(toString, method);
                        var expName = "exp" + System.DateTime.Now.Ticks;
                        var varExp = new VariableDefinition(expName, varExpType);
                        method.Body.Variables.Add(varExp);
                        //exceptionInstructions.Add(Instruction.Create(OpCodes.Stloc, varExp));
                        int idx = 0;
                        var exceptionVarFound = false;
                        for (; idx < method.Body.Variables.Count; ++idx)
                        {
                            if (method.Body.Variables[idx].Name.Equals(expName))
                            {
                                exceptionVarFound = true;
                                break;
                            }
                        }
                        /*
                        if (exceptionVarFound)
                        {
                            exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc_S, method.Body.Variables[idx]));
                        }
                        */
                        //exceptionInstructions.Add(Instruction.Create(OpCodes.Stloc_S, method.Body.Variables[idx]));
                        //exceptionInstructions.Add(Instruction.Create(OpCodes.Nop));
                        //exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc_S, method.Body.Variables[idx]));
                        //exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc_S, method.Body.Variables[idx]));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Call, exception));
                    }

                    var after = this.FindMethodReference(method, aspects[i], Buffalo.Enums.BoundaryType.After);
                    if (after != null)
                    {
                        afterInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        //afterInstructions.Add(Instruction.Create(OpCodes.Ldloc_0));
                        afterInstructions.Add(Instruction.Create(OpCodes.Call, after));
                    }
                }

                int beforeIdx = mdIdx;
                //perform this only if user overrides Before() in the aspect
                if (beforeInstructions.Count > 0)
                {
                    beforeInstructions.ForEach(x => method.Body.Instructions.Insert(beforeIdx++, x));
                }

                /* the last instruction after success should jump to return, or 3 instruction before
                 * return if return type is not void, or as an optimization, maybe we can even skip
                 * directly to last() - 2?
                 */
                var successRet = voidType ? method.Body.Instructions.Last() :
                    method.Body.Instructions[method.Body.Instructions.Count - 3];
                Instruction successLeave = il.Create(OpCodes.Leave, successRet);
                ///TODO: need to double check the format of the MSIL return
                ///br.s, ldloc, ret when return type is not void, thereby decrement by 3
                int successIdx = voidType ? method.Body.Instructions.Count - 1 : method.Body.Instructions.Count - 3;
                //perform this only if user overrides Success() in the aspect
                successInstructions.Add(successLeave);
                if (successInstructions.Count > 0)
                {
                    successInstructions.ForEach(x => method.Body.Instructions.Insert(successIdx++, x));
                }

                int exceptionIdx = voidType ? method.Body.Instructions.Count - 1 : method.Body.Instructions.Count - 3;
                int exceptionIdxConst = exceptionIdx;
                var exceptionRet = voidType ? method.Body.Instructions.Last() :
                    method.Body.Instructions[method.Body.Instructions.Count - 3];
                Instruction exceptionLeave = il.Create(OpCodes.Leave, exceptionRet);
                //perform this only if user overrides Exception() in the aspect
                if (exceptionInstructions.Count > 0)
                {
                    exceptionInstructions.Add(exceptionLeave);
                    exceptionInstructions.ForEach(x => method.Body.Instructions.Insert(exceptionIdx++, x));
                }

                var afterRet = voidType ? method.Body.Instructions.Last() :
                    method.Body.Instructions[method.Body.Instructions.Count - 3];
                var endfinally = il.Create(OpCodes.Endfinally);
                int afterIdx = voidType ? method.Body.Instructions.Count - 1 : method.Body.Instructions.Count - 3;
                int afterIdxConst = afterIdx;
                //perform this only if user overrides After() in the aspect
                if (afterInstructions.Count > 0)
                {
                    afterInstructions.Add(endfinally);
                    afterInstructions.ForEach(x => method.Body.Instructions.Insert(afterIdx++, x));
                }

                #endregion

                #region Catch..Finally..
                //add the catch block only if user overrides Exception() in the aspect
                if (exceptionInstructions.Count > 0)
                {
                    var catchHandler = new ExceptionHandler(ExceptionHandlerType.Catch)
                    {
                        TryStart = method.Body.Instructions.First(),
                        TryEnd = successLeave.Next,
                        HandlerStart = method.Body.Instructions[exceptionIdxConst],
                        HandlerEnd = exceptionLeave.Next,
                        CatchType = this.AssemblyDefinition.MainModule.Import(typeof(Exception)),
                    };
                    method.Body.ExceptionHandlers.Add(catchHandler);
                }

                //add the finally block only if user overrides After() in the aspect
                if (afterInstructions.Count > 0)
                {
                    var finallyHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
                    {
                        TryStart = method.Body.Instructions.First(),
                        TryEnd = method.Body.Instructions[afterIdxConst],
                        HandlerStart = method.Body.Instructions[afterIdxConst],
                        HandlerEnd = afterRet,
                        CatchType = null,
                    };
                    method.Body.ExceptionHandlers.Add(finallyHandler);
                }
                #endregion
                method.Body.OptimizeMacros();
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

        /*
        private static void LoadAssembly()
        {
            ///TODO: need to pass vars to and from appdomains: http://stackoverflow.com/a/1250847/150607
            var _assemblyPath = @"C:\Users\wliao\Documents\Visual Studio 2010\Projects\buffalo\client\bin\Debug\client.exe";
            var assembly = System.Reflection.Assembly.LoadFrom(_assemblyPath);
            var types = assembly.GetTypes().ToList();
        }
        */

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