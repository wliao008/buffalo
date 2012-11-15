using Buffalo;
using System;

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
                return new decimal(-1);
        }
    }

    public class Trace : MethodBoundaryAspect
    {
        public override void Before(MethodArgs args)
        {
            Console.WriteLine(args.FullName);
        }

        public override void Exception(MethodArgs args)
        {
            Console.WriteLine("Exception occured in {0}", args.FullName);
            Console.WriteLine(args.Exception.ToString());
            foreach (var p in args.Parameters)
            {
                Console.WriteLine("\t{0} ({1}): {2}", p.Name, p.Type, p.Value);
            }
        }
    }
}
