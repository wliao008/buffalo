using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Buffalo
{
    public class Enums
    {
        internal enum Status
        {
            Applied,
            NotApplied,
            Excluded
        }

        internal enum BASE
        {
            Before,
            After,
            Success,
            Exception
        }
    }
}
