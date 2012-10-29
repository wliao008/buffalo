using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

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
    }
}
