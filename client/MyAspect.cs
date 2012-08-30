﻿using System;
using System.Diagnostics;
using System.Threading;
using Buffalo;

namespace client
{
    [MyAspect(AttributeExclude = true)]
    [TraceAspect(AttributeExclude = true)]
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

        public override void Before(MethodDetail detail)
        {
            watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            Console.WriteLine("MyAspect.Before: {0}: {1}", detail.ToString(), detail.Name);
            if (detail.Parameters.Count > 0)
            {
                Console.WriteLine("\t******** PARAMETERS ************");
                foreach (var d in detail.Parameters)
                {
                    Console.WriteLine("\t" + d.Key);
                }
            }
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
