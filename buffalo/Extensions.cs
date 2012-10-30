using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Text;
using System.Collections.Generic;

namespace Buffalo
{
    internal static class Extensions
    {
        internal static MethodReference FindMethodReference(this MethodDefinition method, Aspect aspect, Buffalo.Enums.AspectType name)
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

            //loop thru the parameters and extract the value
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

                var pType = method.Parameters[i].ParameterType;
                if (pType.IsByReference)
                {
                    typeSpec = pType as TypeSpecification;
                    if (typeSpec != null)
                    {
                        switch (typeSpec.ElementType.MetadataType)
                        {
                            case MetadataType.Int32:
                                instructions.Add(Instruction.Create(OpCodes.Ldind_I4));
                                break;
                        }
                    }
                }

                if (pType.IsValueType)
                {
                    instructions.Add(Instruction.Create(OpCodes.Box, pType));
                }

                instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            }

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
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            var maSetPropertiesRef = assemblyDef.MainModule.Import(maSetProperties, method);
            instructions.Add(Instruction.Create(OpCodes.Callvirt, maSetPropertiesRef));

            int idx = 0;
            instructions.ForEach(x => method.Body.Instructions.Insert(idx++, x));

            var.Var = varMa;
            var.VarIdx = idx;
            return var;
        }
    }

    internal class VariableResult
    {
        public VariableDefinition Var { get; set; }
        public int VarIdx { get; set; }
    }
}
