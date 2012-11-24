using System;
using Buffalo.Common;
using Mono.Cecil;

namespace Buffalo
{
    internal class Aspect : IAspect
    {
        public Aspect()
        {
            this.AssemblyLevelStatus = Enums.Status.NotApplied;
        }

        public string Name { get; set; }

        public Enums.Status AssemblyLevelStatus { get; set; }

        public TypeDefinition TypeDefinition { get; set; }

        public Buffalo.Common.Enums.BuffaloAspect BuffaloAspect { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
