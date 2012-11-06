using System;
using System.Diagnostics;
using System.Threading;
using Buffalo;

namespace client
{
    public class DoubleAspect : MethodAroundAspect
    {
        public override object Invoke(MethodArgs args)
        {
            return args.Proceed();
        }
    }
}
