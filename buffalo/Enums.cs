using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Buffalo
{
    internal class Enums
    {
        internal enum Status
        {
            Applied,
            NotApplied,
            Excluded
        }

        internal enum BoundaryType
        {
            Before,
            After,
            Success,
            Exception,
            Invoke
        }
    }
}
