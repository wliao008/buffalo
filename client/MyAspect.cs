using System;
using System.Diagnostics;
using System.Threading;
using Buffalo;

namespace client
{
    [MyAspect(AttributeExclude = true)]
    [TraceAspect(AttributeExclude = true)]
    //[MyFakeAddAspect(AttributeExclude = true)]
    //[MyAroundAspect(AttributeExclude = true)]
    public class MyAspect : MethodBoundaryAspect
    {
        public Stopwatch watch;
        public static Random rand;
        static MyAspect()
        {
            rand = new Random((int)DateTime.Now.Ticks);
        }

        //public MyAspect()
        //{
        //    watch = new Stopwatch();
        //    watch.Start();
        //}

        public override void Before()
        {
            watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            Console.WriteLine("MyAspect.Before");
            //if (detail.Parameters.Count > 0)
            //{
            //    Console.WriteLine("\t******** PARAMETERS ************");
            //    foreach (var d in detail.Parameters)
            //    {
            //        Console.WriteLine("\t{0} ({1})", d.Name, d.Type);
            //    }
            //}
            //Console.WriteLine("MyAspect.Before, sleeping for: {0} ms", i);
            //Thread.Sleep(i);
        }

        public override void After()
        {
            watch.Stop();
            Console.WriteLine("MyAspect.After, times passed: {0} s {1} ms", watch.Elapsed.Seconds, watch.Elapsed.Milliseconds);
        }

        public override void Success()
        {
            Console.WriteLine("MyAspect.Success");
        }

        public override void Exception()
        {
            Console.WriteLine("MyAspect.Exception");
        }
    }

    [MyAspect(AttributeExclude = true)]
    [TraceAspect(AttributeExclude = true)]
    //[MyFakeAddAspect(AttributeExclude = true)]
    //[MyAroundAspect(AttributeExclude = true)]
    public class TraceAspect : MethodBoundaryAspect
    {
        public override void Before()
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

    /*
    [MyAspect(AttributeExclude = true)]
    [TraceAspect(AttributeExclude = true)]
    [MyAroundAspect(AttributeExclude = true)]
    [MyFakeAddAspect(AttributeExclude = true)]
    public class MyAroundAspect : MethodAroundAspect
    {
        Random r = new Random((int)DateTime.Now.Ticks);

        public override void Invoke(MethodDetail detail)
        {
            //int a = 0;
            //if (a == 1)
            //{
            //    detail.Proceed();
            //}
            //else
            //{
            //    Console.WriteLine("Never called");
            //}
            Console.WriteLine("Call original method only if tick is even...");
            var num = r.Next(0, 10);
            var call = num % 2;
            Console.WriteLine("num: " + num);
            if (call == 0)
            {
                Console.WriteLine("Calling original method...");
                detail.Proceed();
            }
            else
            {
                Console.WriteLine("can't call since it's odd");
            }
        }
    }

    [MyAspect(AttributeExclude = true)]
    [TraceAspect(AttributeExclude = true)]
    [MyAroundAspect(AttributeExclude = true)]
    [MyFakeAddAspect(AttributeExclude = true)]
    public class MyFakeAddAspect : MethodAroundAspect
    {
        public override void Invoke(MethodDetail detail)
        {
            Console.WriteLine("7");
        }
    }
    */
}
