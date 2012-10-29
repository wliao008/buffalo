using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Text;

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

        internal static VariableDefinition AddMethodArgsVariable(this MethodDefinition method,
            AssemblyDefinition assemblyDef)
        {
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
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, maCtrRef));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, varMa));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, varMa));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, method.Name));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, method.FullName));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, method.ReturnType.FullName));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, sb.ToString()));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            var maSetPropertiesRef = assemblyDef.MainModule.Import(maSetProperties, method);
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, maSetPropertiesRef));

            return varMa;
        }
    }
}
