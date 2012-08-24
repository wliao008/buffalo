using System;
using Mono.Cecil;

namespace Buffalo
{
    internal class Aspect : IAspect
    {
        public Aspect()
        {
            this.AssemblyLevelStatus = Status.NotApplied;
        }

        public string Name { get; set; }

        public Status AssemblyLevelStatus { get; set; }

        public TypeDefinition TypeDefinition { get; set; }

        public System.Type Type { get; set; }

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
