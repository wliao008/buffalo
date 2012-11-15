using Buffalo.Common;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Buffalo.Extensions
{
    internal static class Extensions
    {
        internal static MethodReference FindMethodReference(this MethodDefinition method, Aspect aspect, Enums.AspectType name)
        {
            return aspect
                .TypeDefinition
                .Methods
                .FirstOrDefault(x => x.Name.Equals(name.ToString()));
        }

        internal static VariableResult AddMethodArgsVariable(this MethodDefinition method,
            AssemblyDefinition assemblyDef)
        {
            var var = new VariableResult();
            var instructions = new List<Instruction>();
            method.Body.InitLocals = true;
            var il = method.Body.GetILProcessor();
            var isValueType = false;
            
            //create var to hold parameter count
            var pcVar = new VariableDefinition("pc" + DateTime.Now.Ticks,
                assemblyDef.MainModule.Import(typeof(int)));
            method.Body.Variables.Add(pcVar);
            instructions.Add(Instruction.Create(OpCodes.Ldc_I4, method.Parameters.Count));
            instructions.Add(Instruction.Create(OpCodes.Stloc, pcVar));

            //create an object array to hold the parameter values
            var objType = assemblyDef.MainModule.Import(typeof(object));
            var objArray = new ArrayType(objType);
            var varArray = new VariableDefinition("va" + DateTime.Now.Ticks,
                (TypeReference)objArray);
            method.Body.Variables.Add(varArray);
            instructions.Add(Instruction.Create(OpCodes.Ldloc, pcVar));
            instructions.Add(Instruction.Create(OpCodes.Newarr, objType));
            instructions.Add(Instruction.Create(OpCodes.Stloc, varArray));

            #region Handle paramters
            //loop thru the parameters, extract the values and 
            //store them in MethodArgs.ParameterArray
            TypeSpecification typeSpec = null;
            for (int i = 0; i < method.Parameters.Count; ++i )
            {
                var metaType = method.Parameters[i].ParameterType.MetadataType;
                if (metaType == MetadataType.UIntPtr
                    || metaType == MetadataType.FunctionPointer
                    || metaType == MetadataType.IntPtr
                    || metaType == MetadataType.Pointer)
                    continue;

                instructions.Add(Instruction.Create(OpCodes.Ldloc, varArray));
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4, i));
                if (method.IsStatic)
                    instructions.Add(il.Create(OpCodes.Ldarg, i));
                else
                    instructions.Add(il.Create(OpCodes.Ldarg, i + 1));

                isValueType = false;
                var pType = method.Parameters[i].ParameterType;
                if (pType.IsByReference)
                {
                    typeSpec = pType as TypeSpecification;
                    if (typeSpec != null)
                    {
                        switch (typeSpec.ElementType.MetadataType)
                        {
                            case MetadataType.Boolean:
                            case MetadataType.SByte:
                                instructions.Add(Instruction.Create(OpCodes.Ldind_I1)); 
                                isValueType = true;
                                break;
                            case MetadataType.Int16:
                                instructions.Add(Instruction.Create(OpCodes.Ldind_I2)); 
                                isValueType = true;
                                break;
                            case MetadataType.Int32:
                                instructions.Add(Instruction.Create(OpCodes.Ldind_I4)); 
                                isValueType = true;
                                break;
                            case MetadataType.Int64:
                            case MetadataType.UInt64:
                                instructions.Add(Instruction.Create(OpCodes.Ldind_I8)); 
                                isValueType = true;
                                break;
                            case MetadataType.Byte:
                                instructions.Add(Instruction.Create(OpCodes.Ldind_U1)); 
                                isValueType = true;
                                break;
                            case MetadataType.UInt16:
                                instructions.Add(Instruction.Create(OpCodes.Ldind_U2)); 
                                isValueType = true;
                                break;
                            case MetadataType.UInt32:
                                instructions.Add(Instruction.Create(OpCodes.Ldind_U4)); 
                                isValueType = true;
                                break;
                            case MetadataType.Single:
                                instructions.Add(Instruction.Create(OpCodes.Ldind_R4));
                                isValueType = true;
                                break;
                            case MetadataType.Double:
                                instructions.Add(Instruction.Create(OpCodes.Ldind_R8));
                                isValueType = true;
                                break;
                            case MetadataType.IntPtr:
                            case MetadataType.UIntPtr:
                                instructions.Add(Instruction.Create(OpCodes.Ldind_I));
                                isValueType = true;
                                break;
                            default:
                                if (typeSpec.ElementType.IsValueType)
                                {
                                    instructions.Add(Instruction.Create(OpCodes.Ldobj, typeSpec.ElementType));
                                    isValueType = true;
                                }
                                else
                                {
                                    instructions.Add(Instruction.Create(OpCodes.Ldind_Ref));
                                    isValueType = false;
                                }
                                break;
                        }
                    }
                }

                if (pType.IsValueType || isValueType)
                {
                    if(isValueType)
                        instructions.Add(Instruction.Create(OpCodes.Box, typeSpec.ElementType));
                    else
                        instructions.Add(Instruction.Create(OpCodes.Box, pType));
                }

                instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            }
            #endregion

            StringBuilder sb = new StringBuilder();
            method.Parameters.ToList()
                .ForEach(x =>
                {
                    sb.Append(string.Format("{0}:{1}|", x.Name, x.ParameterType.FullName));
                });

            var maType = typeof(MethodArgs);
            var maName = "ma" + DateTime.Now.Ticks;
            var maSetProperties = maType.GetMethod("SetProperties");
            var varMa = new VariableDefinition(maName, assemblyDef.MainModule.Import(maType));
            method.Body.Variables.Add(varMa);
            var vaMaIdx = method.Body.Variables.Count - 1;
            var maCtr = maType.GetConstructor(new Type[] { });
            MethodReference maCtrRef = assemblyDef.MainModule.Import(maCtr);
            instructions.Add(Instruction.Create(OpCodes.Newobj, maCtrRef));
            instructions.Add(Instruction.Create(OpCodes.Stloc, varMa));
            instructions.Add(Instruction.Create(OpCodes.Ldloc, varMa));
            instructions.Add(Instruction.Create(OpCodes.Ldstr, method.Name));
            instructions.Add(Instruction.Create(OpCodes.Ldstr, method.FullName));
            instructions.Add(Instruction.Create(OpCodes.Ldstr, method.ReturnType.FullName));
            instructions.Add(Instruction.Create(OpCodes.Ldstr, sb.ToString()));
            instructions.Add(Instruction.Create(OpCodes.Ldloc, varArray));
            if (method.IsStatic)
                instructions.Add(Instruction.Create(OpCodes.Ldnull));
            else
                instructions.Add(Instruction.Create(OpCodes.Ldarg_0));

            var maSetPropertiesRef = assemblyDef.MainModule.Import(maSetProperties, method);
            instructions.Add(Instruction.Create(OpCodes.Callvirt, maSetPropertiesRef));

            int idx = 0;
            instructions.ForEach(x => method.Body.Instructions.Insert(idx++, x));

            var.Var = varMa;
            var.ParamArray = varArray;
            var.VarIdx = idx;
            return var;
        }
    }

    internal class VariableResult
    {
        public VariableDefinition Var { get; set; }
        public VariableDefinition ParamArray { get; set; }
        public int VarIdx { get; set; }
    }
}
