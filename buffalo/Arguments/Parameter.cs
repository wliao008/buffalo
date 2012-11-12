using System;

namespace Buffalo.Arguments
{
    public sealed class Parameter
    {
        public Type Type { get; set; }

        public string Name { get; set; }

        public object Value { get; set; }
    }
}
