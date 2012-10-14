using System;
using Mono.Cecil;

namespace BuffaloAOP
{
    internal class Aspect : IAspect
    {
        public Aspect()
        {
            this.AssemblyLevelStatus = BuffaloAOP.Enums.Status.NotApplied;
        }

        public string Name { get; set; }

        public BuffaloAOP.Enums.Status AssemblyLevelStatus { get; set; }

        public TypeDefinition TypeDefinition { get; set; }

        public System.Type Type { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
