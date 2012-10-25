using System;
using System.Diagnostics;
using System.Threading;
using Buffalo;

namespace client
{
    [TraceAspect(AttributeExclude = true)]
    public class TraceAspect : MethodBoundaryAspect
    {
        public override void Before(string name, string fullname)
        {
            Console.WriteLine("Trace.Before: " + name+", " + fullname);
        }

        //public override void Before(MethodDetail detail)
        //{
        //    Console.WriteLine("Trace.Before: " + detail.Name);
        //}

        public override void Exception()
        {
            Console.WriteLine("********* TRACE EXCEPTION!! ********");
        }

        public override void Success()
        {
            Console.WriteLine("Trace.Success");
        }

        //public override void After()
        //{
        //    Console.WriteLine("Trace.After");
        //}
    }
}
