using System;
using System.Diagnostics;
using System.Threading;
using Buffalo;

namespace client
{
    public class DoubleAspect : MethodAroundAspect
    {
        [Trace(AttributeExclude = true)]
        [DoubleAspect(AttributeExclude = true)]
        public override object Invoke(MethodArgs args)
        {
            Console.WriteLine("Enter a num: ");
            int num = int.Parse(Console.ReadLine());
            if (num % 2 == 0)
                return args.Proceed();
            else
                return -1.0;
        }
    }

    [Trace(AttributeExclude = true)]
    [DoubleAspect(AttributeExclude = true)]
    public class Trace : MethodBoundaryAspect
    {
        public override void Before(MethodArgs args)
        {
            Console.WriteLine(args.Name);
        }
    }
}
