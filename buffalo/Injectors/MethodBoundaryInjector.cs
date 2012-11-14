using Buffalo.Arguments;
using Buffalo.Common;
using Buffalo.Extensions;
using Buffalo.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
//using Mono.Cecil.Rocks;

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
            var eligibleBoundaryMethods = ems.Where(x => x.Value.Any(y => y.Type.BaseType == typeof(MethodBoundaryAspect)));
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

                //method.Body.SimplifyMacros();
                #region Method detail
                //create a MethodArgs
                var var = method.AddMethodArgsVariable(this.AssemblyDefinition);
                #endregion

                #region Before, Success, Exception, After
                for (int i = 0; i < aspects.Count; ++i)
                {
                    #region Create an aspect variable
                    //var varAspectType = this.AssemblyDefinition.MainModule.Import(typeof(MethodBoundaryAspect));
                    var varAspectName = "asp" + System.DateTime.Now.Ticks;
                    var varAspect = new VariableDefinition(varAspectName, aspects[i].TypeDefinition);
                    method.Body.Variables.Add(varAspect);
                    var varAspectIdx = method.Body.Variables.Count - 1;
                    //call ctor
                    Type t = aspects[i].Type;
                    var constructorInfo = t.GetConstructor(new Type[] { });
                    MethodReference myClassConstructor = this.AssemblyDefinition.MainModule.Import(constructorInfo);
                    aspectVarInstructions.Add(Instruction.Create(OpCodes.Newobj, myClassConstructor));
                    aspectVarInstructions.Add(Instruction.Create(OpCodes.Stloc, varAspect));
                    #endregion

                    #region Before, success, exception
                    var before = method.FindMethodReference(aspects[i], Enums.AspectType.Before);
                    if (before != null)
                    {

                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldloc, varAspect));
                        beforeInstructions.Add(Instruction.Create(OpCodes.Ldloc, var.Var));
                        var aspectBefore = aspects[i].Type.GetMethod("Before");
                        var aspectBeforeRef = this.AssemblyDefinition.MainModule.Import(aspectBefore, method);
                        beforeInstructions.Add(Instruction.Create(OpCodes.Callvirt, aspectBeforeRef));
                    }

                    var success = method.FindMethodReference(aspects[i], Enums.AspectType.Success);
                    if (success != null)
                    {
                        successInstructions.Add(Instruction.Create(OpCodes.Ldloc, varAspect));
                        successInstructions.Add(Instruction.Create(OpCodes.Ldloc, var.Var));
                        var aspectSuccess = aspects[i].Type.GetMethod("Success");
                        var aspectSuccessRef = this.AssemblyDefinition.MainModule.Import(aspectSuccess, method);
                        successInstructions.Add(Instruction.Create(OpCodes.Callvirt, aspectSuccessRef));
                    }

                    var exception = method.FindMethodReference(aspects[i], Enums.AspectType.Exception);
                    if (exception != null)
                    {
                        var varExpType = typeof(System.Exception);
                        var expName = "exp" + System.DateTime.Now.Ticks;
                        var varExp = new VariableDefinition(expName, this.AssemblyDefinition.MainModule.Import(varExpType));
                        method.Body.Variables.Add(varExp);
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Stloc, varExp));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc, var.Var));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc, varExp));
                        var maSetException = typeof(MethodArgs).GetMethod("SetException");
                        var maSetExceptionRef = this.AssemblyDefinition.MainModule.Import(maSetException, method);
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Callvirt, maSetExceptionRef));

                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc, varAspect));
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Ldloc, var.Var));
                        var aspectException = aspects[i].Type.GetMethod("Exception");
                        //var aspectBefore = varAspectType.Resolve().Methods.FirstOrDefault(x => x.Name.Equals("Before"));
                        var aspectExceptionRef = this.AssemblyDefinition.MainModule.Import(aspectException, method);
                        exceptionInstructions.Add(Instruction.Create(OpCodes.Callvirt, aspectExceptionRef));
                    }

                    var after = method.FindMethodReference(aspects[i], Enums.AspectType.After);
                    if (after != null)
                    {
                        afterInstructions.Add(Instruction.Create(OpCodes.Ldloc, varAspect));
                        afterInstructions.Add(Instruction.Create(OpCodes.Ldloc, var.Var));
                        var aspectAfter = aspects[i].Type.GetMethod("After");
                        var aspectAfterRef = this.AssemblyDefinition.MainModule.Import(aspectAfter, method);
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
                        CatchType = this.AssemblyDefinition.MainModule.Import(typeof(Exception)),
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
                //method.Body.OptimizeMacros();
            }
        }
    }
}
