using System.Collections.Generic;
using Mono.Cecil;
using System.Linq;
using System;
using Mono.Cecil.Cil;
using System.Text;
using System.Collections.Specialized;
using Mono.Cecil.Rocks;

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
            var NewMethodNames = new StringCollection();
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
                    var varTicks = System.DateTime.Now.Ticks;

                    //create a replacement for the annotated function
                    var methodName = string.Format("{0}{1}", method.Name, varTicks);
                    MethodDefinition newmethod =
                        new MethodDefinition(methodName, method.Attributes, method.ReturnType);
                    methodType.Methods.Add(newmethod);
                    NewMethodNames.Add(methodName);
                    newmethod.Body.SimplifyMacros();

                    //create aspect variable
                    var varAspectName = "asp" + varTicks;
                    var varAspect = new VariableDefinition(varAspectName, aspect.TypeDefinition);
                    newmethod.Body.Variables.Add(varAspect);
                    var varAspectIdx = newmethod.Body.Variables.Count - 1;
                    var constructorInfo = aspect.Type.GetConstructor(new Type[] { });
                    MethodReference myClassConstructor =
                        this.AssemblyDefinition.MainModule.Import(constructorInfo);
                    //store the newly created aspect variable
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, myClassConstructor));
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, varAspect));
                    //copy all the paramters
                    method.Parameters.ToList().ForEach(x =>
                        newmethod.Parameters.Add(new ParameterDefinition(x.Name, x.Attributes, x.ParameterType)));
                    //create a MethodArgs
                    var var = newmethod.AddMethodArgsVariable(this.AssemblyDefinition);

                    #region Calling MethodArgs.Invoke
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, varAspect));
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, var.Var));
                    var aspectInvoke = aspect.Type.GetMethod("Invoke");
                    var aspectInvokeRef = 
                        this.AssemblyDefinition.MainModule.Import(aspectInvoke, newmethod);
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, aspectInvokeRef));
                    #endregion

                    if (!newmethod.ReturnType.FullName.Equals("System.Void"))
                    {
                        //create an object variable to hold the return value
                        var varObj = new VariableDefinition("obj" + varTicks,
                            this.AssemblyDefinition.MainModule.Import(typeof(object)));
                        newmethod.Body.Variables.Add(varObj);
                        newmethod.Body.Instructions.Add(
                            Instruction.Create(OpCodes.Stloc, varObj));
                        newmethod.Body.Instructions.Add(
                            Instruction.Create(OpCodes.Ldloc, varObj));
                    }
                    else
                    {
                        //pop the return value since it's not used?
                        newmethod.Body.Instructions.Add(
                            Instruction.Create(OpCodes.Pop));
                    }

                    #region Modify all calls from origin to the generated method
                    foreach (var type in this.AssemblyDefinition.MainModule.Types)
                    {
                        foreach (var m in type.Methods.Where(x => !NewMethodNames.Contains(x.Name)))
                        {
                            for (int j = 0; j < m.Body.Instructions.Count; ++j)
                            {
                                if (m.Body.Instructions[j].ToString().Contains(method.FullName))
                                {
                                    m.Body.Instructions[j].Operand = newmethod;
                                    //MethodArgs.Invoke returns an object, need to unbox it here to the original type
                                    //However, unboxing is needed only if the original method has a return type
                                    //other than void
                                    if (!newmethod.ReturnType.FullName.Equals("System.Void"))
                                    {
                                        var unbox = Instruction.Create(OpCodes.Unbox_Any, newmethod.ReturnType);
                                        var il2 = m.Body.GetILProcessor();
                                        il2.InsertAfter(m.Body.Instructions[j], unbox);
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    newmethod.Body.OptimizeMacros();
                }
            }
        }
    }
}
