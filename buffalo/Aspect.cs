using System;
using Mono.Cecil;

namespace Buffalo
{
    internal class Aspect : IAspect
    {
        public Aspect()
        {
            this.AssemblyLevelStatus = Buffalo.Enums.Status.NotApplied;
        }

        public string Name { get; set; }

        public Buffalo.Enums.Status AssemblyLevelStatus { get; set; }

        public TypeDefinition TypeDefinition { get; set; }

        public System.Type Type { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
