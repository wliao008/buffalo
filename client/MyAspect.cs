using System;
using System.Diagnostics;
using Buffalo;

namespace client
{
    [MyAspect(AttributeExclude = true)]
    [TraceAspect(AttributeExclude = true)]
    public class MyAspect : MethodBoundaryAspect
    {
        public static Stopwatch watch;
        static MyAspect()
        {
            watch = new Stopwatch();
        }

        public override void Before(MethodDetail detail)
        {
            watch.Reset();
            watch.Start();
            Console.WriteLine("MyAspect.Before");
        }

        public override void After()
        {
            Console.WriteLine("MyAspect.After");
        }

        public override void Success()
        {
            watch.Stop();
            Console.WriteLine("MyAspect.Success, times passed: {0} ms", watch.Elapsed.Milliseconds);
        }

        public override void Exception()
        {
            Console.WriteLine("MyAspect.Exception");
        }
    }

    [MyAspect(AttributeExclude = true)]
    [TraceAspect(AttributeExclude = true)]
    public class TraceAspect : MethodBoundaryAspect
    {
        public override void Before(MethodDetail detail)
        {
            Console.WriteLine("Trace.Before");
        }

        public override void Exception()
        {
            Console.WriteLine("Trace.Exception");
        }

        public override void Success()
        {
            Console.WriteLine("Trace.Success");
        }
    }
}
