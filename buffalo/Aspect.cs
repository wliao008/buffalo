using System.Collections.Generic;
using Mono.Cecil;

namespace Buffalo
{
    internal class Aspect : IAspect
    {
        public string Name { get; set; }

        public bool IsAssemblyLevel { get; set; }

        public TypeDefinition TypeDefinition { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }

    internal enum Status
    {
        Applied,
        NotApplied,
        Excluded
    }
}
