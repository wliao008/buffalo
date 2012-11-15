using Buffalo.Extensions;
using Buffalo.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
//using Mono.Cecil.Rocks;

namespace Buffalo.Injectors
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
                    //newmethod.Body.SimplifyMacros();
                    newmethod.Body.InitLocals = true;

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

                    #region Handling return value
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
                        newmethod.Body.Instructions.Add(
                            Instruction.Create(OpCodes.Unbox_Any, newmethod.ReturnType));
                    }
                    else
                    {
                        //pop the return value since it's not used?
                        newmethod.Body.Instructions.Add(
                            Instruction.Create(OpCodes.Pop));
                    }
                    #endregion

                    #region Handling Proceed()
                    var invoke = aspect.TypeDefinition.Methods.FirstOrDefault(
                        x => x.FullName.Contains("::Invoke(Buffalo.MethodArgs)"));
                    bool found = false;
                    int instIdx = 0;
                    for (; instIdx < invoke.Body.Instructions.Count; ++instIdx)
                    {
                        if (invoke.Body.Instructions[instIdx].ToString()
                            .Contains("System.Object Buffalo.MethodArgs::Proceed"))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        var invokeInstructions = new List<Instruction>();
                        //var ins = Instruction.Create(OpCodes.Call, method);
                        //invoke.Body.Instructions[instIdx] = ins;
                        invoke.Body.Instructions.RemoveAt(instIdx);
                        var startIdx = instIdx;

                        //create a var to hold the original method type instance
                        var instance = new VariableDefinition("instance" + DateTime.Now.Ticks,
                            this.AssemblyDefinition.MainModule.Import(typeof(object)));
                        invoke.Body.Variables.Add(instance);
                        invoke.Body.InitLocals = true;

                        //get the instance obj from MethodArgs
                        var getInstance = typeof(MethodArgs).GetMethod("get_Instance");
                        var getInstanceRef = this.AssemblyDefinition.MainModule.Import(getInstance);
                        invokeInstructions.Add(Instruction.Create(OpCodes.Callvirt, getInstanceRef));
                        invokeInstructions.Add(Instruction.Create(OpCodes.Stloc, instance));

                        //create object array to hold ParameterArray
                        var objType = this.AssemblyDefinition.MainModule.Import(typeof(object));
                        var objArray = new ArrayType(objType);
                        var varArray = new VariableDefinition("va" + DateTime.Now.Ticks,
                            (TypeReference)objArray);
                        invoke.Body.Variables.Add(varArray);
                        //invokeInstructions.Add(Instruction.Create(OpCodes.Ldc_I4, method.Parameters.Count));
                        //invokeInstructions.Add(Instruction.Create(OpCodes.Newarr, objType));
                        var getParameterArray = typeof(MethodArgs).GetMethod("get_ParameterArray");
                        var getParameterArrayRef = this.AssemblyDefinition.MainModule.Import(getParameterArray);
                        invokeInstructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                        invokeInstructions.Add(Instruction.Create(OpCodes.Callvirt, getParameterArrayRef));
                        invokeInstructions.Add(Instruction.Create(OpCodes.Stloc, varArray));

                        //modify the Invoke() instruction to make a call to the original method
                        invokeInstructions.Add(Instruction.Create(OpCodes.Ldloc, instance));
                        invokeInstructions.Add(Instruction.Create(OpCodes.Unbox_Any, method.DeclaringType));
                        if (method.Parameters.Count > 0)
                        {
                            for (int i = 0; i < method.Parameters.Count; ++i)
                            {
                                invokeInstructions.Add(Instruction.Create(OpCodes.Ldloc, varArray));
                                invokeInstructions.Add(il.Create(OpCodes.Ldc_I4, i));
                                invokeInstructions.Add(il.Create(OpCodes.Ldelem_Ref));
                                invokeInstructions.Add(Instruction.Create(OpCodes.Unbox_Any, method.Parameters[i].ParameterType));
                            }
                        }

                        //make the call
                        invokeInstructions.Add(Instruction.Create(OpCodes.Callvirt, method));

                        #region Handling return value
                        if (!method.ReturnType.FullName.Equals("System.Void"))
                        {
                            //create an object variable to hold the return value
                            var varObj = new VariableDefinition("obj" + DateTime.Now.Ticks,
                                this.AssemblyDefinition.MainModule.Import(typeof(object)));
                            invoke.Body.Variables.Add(varObj);
                            invokeInstructions.Add(
                                Instruction.Create(OpCodes.Box, method.ReturnType));
                        }
                        else
                        {
                            //method is suppose to return void, but since
                            //previously it calls Proceed() which returns object type,
                            //we need to handle that.
                            invoke.Body.Instructions[instIdx] = Instruction.Create(OpCodes.Nop);
                        }
                        #endregion

                        //write out the instruction
                        invokeInstructions.ForEach(
                            x => invoke.Body.Instructions.Insert(startIdx++, x));
                    }
                    #endregion

                    #region Modify all calls from origin to the generated method
                    foreach (var type in this.AssemblyDefinition.MainModule.Types
                        .Where(x => x.BaseType == null || !x.BaseType.FullName.Equals("Buffalo.MethodAroundAspect")))
                    {
                        var methods = type.Methods.Where(x => !NewMethodNames.Contains(x.Name));
                        foreach (var m in methods)
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
                                        //var unbox = Instruction.Create(OpCodes.Unbox_Any, newmethod.ReturnType);
                                        //var il2 = m.Body.GetILProcessor();
                                        //il2.InsertAfter(m.Body.Instructions[j], unbox);
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    //newmethod.Body.OptimizeMacros();
                }
            }
        }
    }
}
