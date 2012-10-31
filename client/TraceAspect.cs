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
            for (int i = 0; i < args.ParameterArray.Length; ++i )
            {
                args.ParameterArray[i] = (int)args.ParameterArray[i] * 2;
            }

            return args.Proceed();
        }
    }
}
