using System.Collections.Generic;
using Mono.Cecil;
using System.Linq;
using System;
using Mono.Cecil.Cil;
using System.Text;

namespace Buffalo
{
    internal class MethodAroundInjector : IInjectable
    {
        AssemblyDefinition AssemblyDefinition;
        Dictionary<MethodDefinition, List<Aspect>> EligibleMethods;

        public void Inject(Mono.Cecil.AssemblyDefinition assemblyDefinition, 
            Dictionary<Mono.Cecil.MethodDefinition, List<Aspect>> eligibleMethods)
        {
            /* The around aspect is to intercept all calls to the target method, and
             * replace those calls with a completely different method. While preserving
             * the option to call back to the target method when necessary.
             */
            this.AssemblyDefinition = assemblyDefinition;
            this.EligibleMethods = eligibleMethods;
            
            var eligibleAroundMethods = this.EligibleMethods
                .Where(x => x.Value.Any(y => y.Type.BaseType == typeof(MethodAroundAspect)));
            foreach (var d in eligibleAroundMethods)
            {
                var method = d.Key;
                var aspects = d.Value;
                var il = method.Body.GetILProcessor();
                var methodType = method.DeclaringType;
                var maInstructions = new List<Instruction>();
                var aspectVarInstructions = new List<Instruction>();
                var aroundInstructions = new List<Instruction>();

                foreach (var aspect in aspects.Where(x => x.Type.BaseType == typeof(MethodAroundAspect)))
                {
                    //create a replacement for the annotated function
                    var varTicks = System.DateTime.Now.Ticks;
                    var methodName = string.Format("{0}{1}", method.Name, varTicks);
                    MethodDefinition newmethod =
                        new MethodDefinition(methodName, method.Attributes, method.ReturnType);
                    methodType.Methods.Add(newmethod);

                    //create aspect variable           
                    var varAspectName = "asp" + varTicks;
                    var varAspect = new VariableDefinition(varAspectName, aspect.TypeDefinition);
                    newmethod.Body.Variables.Add(varAspect);
                    var varAspectIdx = newmethod.Body.Variables.Count - 1;
                    var constructorInfo = aspect.Type.GetConstructor(new Type[] { });
                    MethodReference myClassConstructor =
                        this.AssemblyDefinition.MainModule.Import(constructorInfo);

                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, myClassConstructor));
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, varAspect));

                    #region Method detail
                    StringBuilder sb = new StringBuilder();
                    method.Parameters.ToList()
                        .ForEach(x =>
                        {
                            sb.Append(string.Format("{0}:{1}|", x.Name, x.ParameterType.FullName));
                        });

                    var maType = typeof(MethodArgs);
                    var maName = "ma" + DateTime.Now.Ticks;
                    var maSetProperties = maType.GetMethod("SetProperties");
                    var varMa = new VariableDefinition(maName, this.AssemblyDefinition.MainModule.Import(maType));
                    newmethod.Body.Variables.Add(varMa);
                    var vaMaIdx = newmethod.Body.Variables.Count - 1;
                    var maCtr = maType.GetConstructor(new Type[] { });
                    MethodReference maCtrRef = this.AssemblyDefinition.MainModule.Import(maCtr);
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, maCtrRef));
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, varMa));
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, varMa));
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, method.Name));
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, method.FullName));
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, method.ReturnType.FullName));
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, sb.ToString()));
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                    var maSetPropertiesRef = this.AssemblyDefinition.MainModule.Import(maSetProperties, newmethod);
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, maSetPropertiesRef));
                    #endregion

                    #region Calling MethodArgs.Invoke
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, varAspect));
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, varMa));
                    var aspectInvoke = aspect.Type.GetMethod("Invoke");
                    var aspectInvokeRef = this.AssemblyDefinition.MainModule.Import(aspectInvoke, newmethod);
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, aspectInvokeRef));
                    #endregion

                    #region Modify the around instruction to do Proceed()
                    //var invoke = aspect.TypeDefinition.Methods.FirstOrDefault(
                    //    x => x.FullName.Contains("::Invoke(Buffalo.MethodArgs)"));
                    //int instIdx = 0;
                    //bool found = false;
                    //for (; instIdx < invoke.Body.Instructions.Count; ++instIdx)
                    //{
                    //    if (invoke.Body.Instructions[instIdx].ToString()
                    //        .Contains("System.Object Buffalo.MethodArgs::Proceed"))
                    //    {
                    //        found = true;
                    //        break;
                    //    }
                    //}

                    //if (found)
                    //{
                    //    //make a call to the original method
                    //    var ldarg0 = Instruction.Create(OpCodes.Ldarg_0);
                    //    invoke.Body.Instructions[instIdx - 1] = ldarg0;
                    //    invoke.Body.Instructions[instIdx] = Instruction.Create(OpCodes.Call, method);
                    //    int count = instIdx;
                    //    if (method.Parameters.Count > 0)
                    //    {
                    //        for (int j = 0; j < method.Parameters.Count; ++j)
                    //        {
                    //            var ins = Instruction.Create(OpCodes.Ldarg, method.Parameters[j]);
                    //            newmethod.Body.Instructions.Insert(count++, ins);
                    //        }
                    //    }
                    //}
                    #endregion

                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                }
                //maInstructions.ForEach(x => newmethod.Body.Instructions.Insert(varIdx++, x));
                //aspectVarInstructions.ForEach(x => newmethod.Body.Instructions.Insert(varIdx++, x));
                //aroundInstructions.ForEach(x => newmethod.Body.Instructions.Insert(varIdx++, x));


                //var aroundAspect = aspects
                //    .SingleOrDefault(x => x.Type.BaseType == typeof(MethodAroundAspect));
                //var aroundMethod = aroundAspect.TypeDefinition.Methods
                //    .SingleOrDefault(x => x.FullName.Contains("Invoke(Buffalo.MethodArgs)"));
                //var instProceed = aroundMethod.Body.Instructions
                //    .FirstOrDefault(x => x.ToString()
                //        .Contains("callvirt System.Void Buffalo.MethodArgs::Proceed()"));
                //MethodDefinition newmethod = 
                //    new MethodDefinition(methodName, method.Attributes, method.ReturnType);
                //methodType.Methods.Add(newmethod);

                //#region Method detail
                //StringBuilder sb = new StringBuilder();
                //method.Parameters.ToList()
                //    .ForEach(x =>
                //    {
                //        sb.Append(string.Format("{0}:{1}|", x.Name, x.ParameterType.FullName));
                //    });

                //var maType = typeof(MethodArgs);
                //var maName = "ma" + DateTime.Now.Ticks;
                //var maSetProperties = maType.GetMethod("SetProperties");
                //var varMa = new VariableDefinition(maName, this.AssemblyDefinition.MainModule.Import(maType));
                //newmethod.Body.Variables.Add(varMa);
                //var vaMaIdx = newmethod.Body.Variables.Count - 1;
                //var maCtr = maType.GetConstructor(new Type[] { });
                //MethodReference maCtrRef = this.AssemblyDefinition.MainModule.Import(maCtr);
                //maInstructions.Add(Instruction.Create(OpCodes.Newobj, maCtrRef));
                //maInstructions.Add(Instruction.Create(OpCodes.Stloc, varMa));
                //maInstructions.Add(Instruction.Create(OpCodes.Ldloc, varMa));
                //maInstructions.Add(Instruction.Create(OpCodes.Ldstr, method.Name));
                //maInstructions.Add(Instruction.Create(OpCodes.Ldstr, method.FullName));
                //maInstructions.Add(Instruction.Create(OpCodes.Ldstr, method.ReturnType.FullName));
                //maInstructions.Add(Instruction.Create(OpCodes.Ldstr, sb.ToString()));
                //var maSetPropertiesRef = this.AssemblyDefinition.MainModule.Import(maSetProperties, newmethod);
                //maInstructions.Add(Instruction.Create(OpCodes.Callvirt, maSetPropertiesRef));
                //#endregion

                //foreach (var aspect in aspects.Where(x => x.Type.BaseType == typeof(MethodAroundAspect)))
                //{
                //    var varTicks = System.DateTime.Now.Ticks;
                //    var aspectInvoke = aspect.Type.GetMethod("Invoke");
                //    var aspectInvokeRef = this.AssemblyDefinition.MainModule.Import(aspectInvoke, newmethod);
                //    var invokeMethod = aspect.TypeDefinition.Methods.FirstOrDefault(x => x.Name.Equals("Invoke"));
                //    //create a customized invoke()
                //    var invokeName = string.Format("{0}{1}", invokeMethod.Name, varTicks);
                //    MethodDefinition newInvoke =
                //        new MethodDefinition(invokeName, invokeMethod.Attributes, aspect.TypeDefinition);
                //    aspect.TypeDefinition.Methods.Add(newInvoke);
                //    //copy from invoke to the new invoke
                //    invokeMethod.Parameters.ToList().ForEach(
                //        x => newInvoke.Parameters.Add(new ParameterDefinition(x.ParameterType)));
                //    invokeMethod.Body.Variables.ToList().ForEach(
                //        x => newInvoke.Body.Variables.Add(new VariableDefinition(x.VariableType)));
                //    invokeMethod.Body.Instructions.ToList().ForEach(
                //        x =>
                //        {
                //            var ctr = typeof(Instruction).GetConstructor(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] { typeof(OpCode), typeof(object) }, null);
                //            var newInstruction = (Instruction)ctr.Invoke(new[] { x.OpCode, x.Operand });
                //            newInvoke.Body.Instructions.Add(x);
                //        });


                //    //create a variable to the around aspect
                //    var varAspectName = "asp" + varTicks;
                //    var varAspect = new VariableDefinition(varAspectName, aspect.TypeDefinition);
                //    newmethod.Body.Variables.Add(varAspect);
                //    var varAspectIdx = newmethod.Body.Variables.Count - 1;
                //    //call ctor
                //    var constructorInfo = aspect.Type.GetConstructor(new Type[] { });
                //    MethodReference myClassConstructor = 
                //        this.AssemblyDefinition.MainModule.Import(constructorInfo);

                //    aspectVarInstructions.Add(Instruction.Create(OpCodes.Newobj, myClassConstructor));
                //    aspectVarInstructions.Add(Instruction.Create(OpCodes.Stloc, varAspect));

                //    //new method instructions
                //    aroundInstructions.Add(Instruction.Create(OpCodes.Ldloc, varAspect));
                //    aroundInstructions.Add(Instruction.Create(OpCodes.Ldloc, varMa));
                //    aroundInstructions.Add(Instruction.Create(OpCodes.Callvirt, aspectInvokeRef));
                //}

                //int varIdx = 0;
                //maInstructions.ForEach(x => newmethod.Body.Instructions.Insert(varIdx++, x));
                //aspectVarInstructions.ForEach(x => newmethod.Body.Instructions.Insert(varIdx++, x));
                //aroundInstructions.ForEach(x => newmethod.Body.Instructions.Insert(varIdx++, x));

            }
        }
    }
}
