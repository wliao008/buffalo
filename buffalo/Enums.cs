using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuffaloAOP
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
