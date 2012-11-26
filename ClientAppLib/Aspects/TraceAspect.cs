using Buffalo;
using System;

namespace ClientAppLib.Aspects
{
    public class TraceAspect : MethodBoundaryAspect
    {
        public override void OnBefore(MethodArgs args)
        {
            Console.WriteLine(args.FullName);
        }

        public override void OnException(MethodArgs args)
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
