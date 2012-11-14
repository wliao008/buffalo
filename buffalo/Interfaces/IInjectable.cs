using System.Collections.Generic;
using Mono.Cecil;

namespace Buffalo.Interfaces
{
    internal interface IInjectable
    {
        void Inject(AssemblyDefinition assemblyDefinition, Dictionary<MethodDefinition, List<Aspect>> eligibleMethods);
    }
}
