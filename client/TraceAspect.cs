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
            Console.WriteLine("Enter a num: ");
            int num = int.Parse(Console.ReadLine());
            if (num % 2 == 0)
                return args.Proceed();
            else
                return -1.0;
        }
    }
}
