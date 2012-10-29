using System.Collections.Generic;
using Mono.Cecil;

namespace Buffalo
{
    internal interface IInjectable
    {
        void Inject(AssemblyDefinition assemblyDefinition, Dictionary<MethodDefinition, List<Aspect>> eligibleMethods);
    }
}
