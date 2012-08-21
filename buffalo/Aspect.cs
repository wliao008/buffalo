using System.Collections.Generic;
using Mono.Cecil;

namespace Buffalo
{
    internal class Aspect : IAspect
    {
        public Aspect()
        {
            this.MethodDefinitions = new List<MethodDefinition>();
        }

        public string Name { get; set; }

        public bool IsAssemblyLevel { get; set; }

        public TypeDefinition TypeDefinition { get; set; }

        public List<MethodDefinition> MethodDefinitions { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
