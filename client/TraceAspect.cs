﻿using Buffalo;
using Buffalo.Arguments;
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
                return -1;
        }
    }

    public class Trace : MethodBoundaryAspect
    {
        public override void Before(MethodArgs args)
        {
            Console.WriteLine(args.FullName);
        }
    }
}
