﻿using Buffalo.Common;
using Buffalo.Extensions;
using Buffalo.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Buffalo.Injectors
{
    internal class MethodBoundaryInjector : IInjectable
    {
        AssemblyDefinition AssemblyDefinition;
        Dictionary<MethodDefinition, List<Aspect>> EligibleMethods;

        public void Inject(AssemblyDefinition assemblyDefinition, Dictionary<MethodDefinition, List<Aspect>> eligibleMethods)
        {
            this.AssemblyDefinition = assemblyDefinition;
            this.EligibleMethods = eligibleMethods;

            var ems = this.EligibleMethods.ToList();
            var eligibleBoundaryMethods = ems.Where(x => x.Value.Any(y =>
                y.BuffaloAspect == Enums.BuffaloAspect.MethodBoundaryAspect));
            foreach (var d in eligibleBoundaryMethods)
            {
                var method = d.Key;
                if (method.Body == null)
                {
#if DEBUG
                    Console.WriteLine(string.Format("{0} has empty body, skipping", method.FullName));
#endif
                    continue;
                }
                var aspects = d.Value;
                var il = method.Body.GetILProcessor();
                var voidType = method.ReturnType.FullName.Equals("System.Void");
                var ret = il.Create(OpCodes.Ret);

                var maInstructions = new List<Instruction>();
                var aspectVarInstructions = new List<Instruction>();
                var beforeInstructions = new List<Instruction>();
                var successInstructions = new List<Instruction>();
                var exceptionInstructions = new List<Instruction>();
                var afterInstructions = new List<Instruction>();

                #region Method detail
                //create a MethodArgs
                var var = method.AddMethodArgsVariable(this.AssemblyDefinition);
                #endregion

                #region Before, Success, Exception, After
                var varExpTypeRef = this.AssemblyDefinition.MainModule.Import(typeof(System.Exception));
                for (int i = 0; i < aspects.Count; ++i)
                {
                    #region Create an aspect variable
                    var varAspectName = "asp" + System.DateTime.Now.Ticks;
                    var varAspectRef = this.AssemblyDefinition.MainModule.Import(aspects[i].TypeDefinition);
                    var varAspect = new VariableDefinition(varAspectName, varAspectRef);
                    method.Body.Variables.Add(varAspect);
                    var varAspectIdx = method.Body.Variables.Count - 1;
                    var ctor = aspects[i].TypeDefinition.Methods.First(x => x.IsConstructor);
                    var ctoref = this.AssemblyDefinition.MainModule.Import(ctor);
                    aspectVarInstructions.Add(Instruction.Create(OpCodes.Newobj, ctoref));
                    aspectVarInstructions.Add(Instruction.Create(OpCodes.Stloc, varAspect));
                    #endregion

                    #region Before, success, exception
                    var before = method.FindMethodReference(aspects[i], Enums.AspectType.OnBefore);
                    if (before != null)
                    {

                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldloc, varAspect));
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldloc, var.Var));
                        var aspectBefore = aspects[i].TypeDefinition.Methods.FirstOrDefault(
                            x => x.Name == Enums.AspectType.OnBefore.ToString());
                        //this import is needed in case this aspect is defined in different assembly?
                        var aspectBeforeRef = this.AssemblyDefinition.MainModule.Import(aspectBefore);
                        beforeInstructions.Add(Instruction.Create(OpCodes.Callvirt, aspectBeforeRef));
                    }

                    var success = method.FindMethodReference(aspects[i], Enums.AspectType.OnSuccess);
                    if (success != null)
                    {
                        successInstructions.Add(Instruction.Create(OpCodes.Ldloc, varAspect));
                        successInstructions.Add(Instruction.Create(OpCodes.Ldloc, var.Var));
                        var aspectSuccess = aspects[i].TypeDefinition.Methods.FirstOrDefault(
                            x => x.Name == Enums.AspectType.OnSuccess.ToString());
                        var aspectSuccessRef = this.AssemblyDefinition.MainModule.Import(aspectSuccess);
                        successInstructions.Add(Instruction.Create(OpCodes.Callvirt, aspectSuccessRef));
                    }

                    var exception = method.FindMethodReference(aspects[i], Enums.AspectType.OnException);
                    if (exception != null)
                    {
                        var expName = "exp" + System.DateTime.Now.Ticks;
                        var varExp = new VariableDefinition(expName, varExpTypeRef);
                        method.Body.Variables.Add(varExp);
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Stloc, varExp));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc, var.Var));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc, varExp));
                        var maSetException = typeof(MethodArgs).GetMethod("SetException");
                        var maSetExceptionRef = this.AssemblyDefinition.MainModule.Import(maSetException, method);
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Callvirt, maSetExceptionRef));

                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc, varAspect));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc, var.Var));
                        var aspectException = aspects[i].TypeDefinition.Methods.FirstOrDefault(
                            x => x.Name == Enums.AspectType.OnException.ToString());
                        var aspectExceptionRef = this.AssemblyDefinition.MainModule.Import(aspectException);
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Callvirt, aspectExceptionRef));
                    }

                    var after = method.FindMethodReference(aspects[i], Enums.AspectType.OnAfter);
                    if (after != null)
                    {
                        afterInstructions.Add(Instruction.Create(OpCodes.Ldloc, varAspect));
                        afterInstructions.Add(Instruction.Create(OpCodes.Ldloc, var.Var));
                        var aspectAfter = aspects[i].TypeDefinition.Methods.FirstOrDefault(
                            x => x.Name == Enums.AspectType.OnAfter.ToString());
                        var aspectAfterRef = this.AssemblyDefinition.MainModule.Import(aspectAfter);
                        afterInstructions.Add(Instruction.Create(OpCodes.Callvirt, aspectAfterRef));
                    }
                    #endregion
                }

                int varIdx = var.VarIdx;
                //maInstructions.ForEach(x => method.Body.Instructions.Insert(varIdx++, x));
                aspectVarInstructions.ForEach(x => method.Body.Instructions.Insert(varIdx++, x));

                int beforeIdx = varIdx;
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
                        TryStart = method.Body.Instructions[varIdx],
                        TryEnd = successLeave.Next,
                        HandlerStart = method.Body.Instructions[exceptionIdxConst],
                        HandlerEnd = exceptionLeave.Next,
                        CatchType = varExpTypeRef,
                    };
                    method.Body.ExceptionHandlers.Add(catchHandler);
                }

                //add the finally block only if user overrides After() in the aspect
                if (afterInstructions.Count > 0)
                {
                    var finallyHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
                    {
                        TryStart = method.Body.Instructions[varIdx],
                        TryEnd = method.Body.Instructions[afterIdxConst],
                        HandlerStart = method.Body.Instructions[afterIdxConst],
                        HandlerEnd = afterRet,
                        CatchType = null,
                    };
                    method.Body.ExceptionHandlers.Add(finallyHandler);
                }
                #endregion
            }
        }
    }
}
