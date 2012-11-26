using Buffalo;
using System;

namespace ClientAppLib.Aspects
{
    public class AroundAspect : MethodAroundAspect
    {
        public override object Invoke(MethodArgs args)
        {
            Console.WriteLine("Enter a num: ");
            int num = int.Parse(Console.ReadLine());
            if (num % 2 == 0)
                return args.Proceed();
            else
                return new decimal(-1);
        }
    }
}
