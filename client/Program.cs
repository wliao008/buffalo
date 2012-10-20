using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffalo;
using System.Threading;

namespace client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Program.Main");
            new Test().TestF();
            Console.Read();
        }
    }

    public class Test
    {
        MethodDetail md = new MethodDetail();
        public Test()
        {
            Console.WriteLine("Test.ctor");
        }

        [TraceAspect(AttributeExclude=true)]
        [MyAspect]
        public void Function1(int a)
        {
            Console.WriteLine("Function 1");
            this.Function3();
        }

        public double Function2(int num1, int num2)
        {
            double result = num1 / num2;
            return result;
        }

        [MyAspect]
        public void Function3()
        {
            Console.WriteLine("Function 3");
        }

        [MyAroundAspect]
        public void TestF()
        {
            Console.WriteLine("testf");
        }
    }
}
