using Buffalo.Extensions;
using Buffalo.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

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
            var eligibleAroundMethods = this.EligibleMethods.Where(x => x.Value.Any(y => 
                    y.BuffaloAspect == Common.Enums.BuffaloAspect.MethodAroundAspect));
            foreach (var d in eligibleAroundMethods)
            {
                var method = d.Key;
                var aspects = d.Value;
                var il = method.Body.GetILProcessor();
                var methodType = method.DeclaringType;
                var maInstructions = new List<Instruction>();
                var aspectVarInstructions = new List<Instruction>();
                var aroundInstructions = new List<Instruction>();

                foreach (var aspec in aspects.Where(x => 
                    x.BuffaloAspect == Common.Enums.BuffaloAspect.MethodAroundAspect))
                {
                    //if aspect is from a different assembly, need to work from that context
                    var aspect = aspec;
                    var writedll = false;
                    AssemblyDefinition ass = null;
                    if (!aspect.TypeDefinition.Module.FullyQualifiedName.Equals(
                        this.AssemblyDefinition.MainModule.FullyQualifiedName))
                    {
                        ass = AssemblyDefinition.ReadAssembly(aspect.TypeDefinition.Module.FullyQualifiedName);
                        var asp = ass.MainModule.Types.FirstOrDefault(x => x.FullName == aspect.Name);
                        if (asp != null)
                        {
                            var newaspect = new Aspect { Name = asp.FullName, TypeDefinition = asp, BuffaloAspect = Common.Enums.BuffaloAspect.MethodAroundAspect };
                            aspect = newaspect;
                            writedll = true;
                        }
                    }

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
                    var varAspectRef = this.AssemblyDefinition.MainModule.Import(aspect.TypeDefinition);
                    var varAspect = new VariableDefinition(varAspectName, varAspectRef);
                    newmethod.Body.Variables.Add(varAspect);
                    var varAspectIdx = newmethod.Body.Variables.Count - 1;
                    var ctor = aspect.TypeDefinition.Methods.First(x => x.IsConstructor);
                    var ctoref = this.AssemblyDefinition.MainModule.Import(ctor);
                    //store the newly created aspect variable
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, ctoref));
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, varAspect));
                    //copy all the paramters
                    method.Parameters.ToList().ForEach(x =>
                        newmethod.Parameters.Add(new ParameterDefinition(x.Name, x.Attributes, x.ParameterType)));
                    //create a MethodArgs
                    var var = newmethod.AddMethodArgsVariable(this.AssemblyDefinition);

                    #region Calling MethodArgs.Invoke
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, varAspect));
                    newmethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, var.Var));
                    var aspectInvoke = aspect.TypeDefinition.Methods.First(x => x.Name.Equals("Invoke"));
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

                    HandleProceed2(method, il, aspect.TypeDefinition);

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
                    if (writedll)
                    {
                        ass.Write2(ass.MainModule.FullyQualifiedName);
                    }
                }
            }
        }

        private void HandleProceed(MethodDefinition method, ILProcessor il, Aspect aspect)
        {
            #region Handling Proceed()
            ///TODO: Proceed() call might appear anywhere in the aspect, not just on the Invoke()!
            var invoke = aspect.TypeDefinition.Methods.FirstOrDefault(
                x => x.FullName.Contains("::Invoke(Buffalo.MethodArgs)"));
            var containProceed = invoke.Body.Instructions.Any(x => x.ToString().Contains("System.Object Buffalo.MethodArgs::Proceed"));

            while (containProceed)
            {
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
                    #region Modified the call
                    var invokeInstructions = new List<Instruction>();
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
                    var getInstanceRef2 = aspect.TypeDefinition.Module.Import(getInstance);
                    invokeInstructions.Add(Instruction.Create(OpCodes.Callvirt, getInstanceRef2));
                    invokeInstructions.Add(Instruction.Create(OpCodes.Stloc, instance));

                    //create object array to hold ParameterArray
                    var objType = this.AssemblyDefinition.MainModule.Import(typeof(object));
                    var objArray = new ArrayType(objType);
                    var varArray = new VariableDefinition("va" + DateTime.Now.Ticks,
                        (TypeReference)objArray);
                    invoke.Body.Variables.Add(varArray);
                    var getParameterArray = typeof(MethodArgs).GetMethod("get_ParameterArray");
                    var getParameterArrayRef = this.AssemblyDefinition.MainModule.Import(getParameterArray);
                    var getParameterArrayRef2 = aspect.TypeDefinition.Module.Import(getParameterArray);
                    invokeInstructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                    invokeInstructions.Add(Instruction.Create(OpCodes.Callvirt, getParameterArrayRef2));
                    invokeInstructions.Add(Instruction.Create(OpCodes.Stloc, varArray));

                    //modify the Invoke() instruction to make a call to the original method
                    invokeInstructions.Add(Instruction.Create(OpCodes.Ldloc, instance));
                    var classref = aspect.TypeDefinition.Module.Import(method.DeclaringType);
                    invokeInstructions.Add(Instruction.Create(OpCodes.Unbox_Any, classref));
                    if (method.Parameters.Count > 0)
                    {
                        for (int i = 0; i < method.Parameters.Count; ++i)
                        {
                            var ptype = aspect.TypeDefinition.Module.Import(method.Parameters[i].ParameterType);
                            invokeInstructions.Add(Instruction.Create(OpCodes.Ldloc, varArray));
                            invokeInstructions.Add(il.Create(OpCodes.Ldc_I4, i));
                            invokeInstructions.Add(il.Create(OpCodes.Ldelem_Ref));
                            invokeInstructions.Add(Instruction.Create(OpCodes.Unbox_Any, ptype));
                        }
                    }

                    #endregion

                    //make the call
                    var methodRef = aspect.TypeDefinition.Module.Import(method);
                    invokeInstructions.Add(Instruction.Create(OpCodes.Callvirt, methodRef));

                    #region Handling return value
                    if (!method.ReturnType.FullName.Equals("System.Void"))
                    {
                        //create an object variable to hold the return value
                        var varObj = new VariableDefinition("obj" + DateTime.Now.Ticks,
                            this.AssemblyDefinition.MainModule.Import(typeof(object)));
                        var retype = aspect.TypeDefinition.Module.Import(method.ReturnType);
                        invoke.Body.Variables.Add(varObj);
                        invokeInstructions.Add(
                            Instruction.Create(OpCodes.Box, retype));
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

                containProceed = invoke.Body.Instructions.Any(x => x.ToString().Contains("System.Object Buffalo.MethodArgs::Proceed"));
            }

            #endregion
        }

        private void HandleProceed2(MethodDefinition method, ILProcessor il, TypeDefinition typedef)
        {
            #region Handling Proceed()
            ///TODO: Proceed() call might appear anywhere in the aspect, not just on the Invoke()!



            foreach (MethodDefinition invoke in typedef.Methods)
            {
                //var invoke = typedef.Methods.FirstOrDefault(
                //    x => x.FullName.Contains("::Invoke(Buffalo.MethodArgs)"));
                if (invoke.Body == null) continue;
                var containProceed = invoke.Body.Instructions.Any(x => x.ToString().Contains("System.Object Buffalo.MethodArgs::Proceed"));

                while (containProceed)
                {
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
                        #region Modified the call
                        var invokeInstructions = new List<Instruction>();
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
                        var getInstanceRef2 = typedef.Module.Import(getInstance);
                        invokeInstructions.Add(Instruction.Create(OpCodes.Callvirt, getInstanceRef2));
                        invokeInstructions.Add(Instruction.Create(OpCodes.Stloc, instance));

                        //create object array to hold ParameterArray
                        var objType = this.AssemblyDefinition.MainModule.Import(typeof(object));
                        var objArray = new ArrayType(objType);
                        var varArray = new VariableDefinition("va" + DateTime.Now.Ticks,
                            (TypeReference)objArray);
                        invoke.Body.Variables.Add(varArray);
                        var getParameterArray = typeof(MethodArgs).GetMethod("get_ParameterArray");
                        var getParameterArrayRef = this.AssemblyDefinition.MainModule.Import(getParameterArray);
                        var getParameterArrayRef2 = typedef.Module.Import(getParameterArray);
                        invokeInstructions.Add(Instruction.Create(OpCodes.Ldarg_1)); //change to ldarg_0 if coming from anonynmous func
                        invokeInstructions.Add(Instruction.Create(OpCodes.Callvirt, getParameterArrayRef2));
                        invokeInstructions.Add(Instruction.Create(OpCodes.Stloc, varArray));

                        //modify the Invoke() instruction to make a call to the original method
                        invokeInstructions.Add(Instruction.Create(OpCodes.Ldloc, instance));
                        var classref = typedef.Module.Import(method.DeclaringType);
                        invokeInstructions.Add(Instruction.Create(OpCodes.Unbox_Any, classref));
                        if (method.Parameters.Count > 0)
                        {
                            for (int i = 0; i < method.Parameters.Count; ++i)
                            {
                                var ptype = typedef.Module.Import(method.Parameters[i].ParameterType);
                                invokeInstructions.Add(Instruction.Create(OpCodes.Ldloc, varArray));
                                invokeInstructions.Add(il.Create(OpCodes.Ldc_I4, i));
                                invokeInstructions.Add(il.Create(OpCodes.Ldelem_Ref));
                                invokeInstructions.Add(Instruction.Create(OpCodes.Unbox_Any, ptype));
                            }
                        }

                        #endregion

                        //make the call
                        var methodRef = typedef.Module.Import(method);
                        invokeInstructions.Add(Instruction.Create(OpCodes.Callvirt, methodRef));

                        #region Handling return value
                        if (!method.ReturnType.FullName.Equals("System.Void"))
                        {
                            //create an object variable to hold the return value
                            var varObj = new VariableDefinition("obj" + DateTime.Now.Ticks,
                                this.AssemblyDefinition.MainModule.Import(typeof(object)));
                            var retype = typedef.Module.Import(method.ReturnType);
                            invoke.Body.Variables.Add(varObj);
                            invokeInstructions.Add(
                                Instruction.Create(OpCodes.Box, retype));
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

                    containProceed = invoke.Body.Instructions.Any(x => x.ToString().Contains("System.Object Buffalo.MethodArgs::Proceed"));
                }
            }


            #endregion

            if (typedef.NestedTypes.Count > 0)
            {
                foreach (TypeDefinition td in typedef.NestedTypes)
                {
                    this.HandleProceed2(method, il, td);
                }
            }
        }
    }
}
