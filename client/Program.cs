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
        static Test test = new Test();
        static void Main(string[] args)
        {
            Console.WriteLine("Program.Main");
            test.TestF1();
            test.TestF2();
            Console.Read();
        }
    }

    public class Test
    {
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
        public void TestF1()
        {
            Console.WriteLine("testf1");
        }

        [MyAroundAspect]
        public void TestF2()
        {
            Console.WriteLine("testf2");
        }

        public void TestF3()
        {
            Console.WriteLine("testf3");
            this.TestF4();
            this.TestF5();
        }

        public void TestF4()
        {
            Console.WriteLine("testf4");
        }

        public void TestF5()
        {
            Console.WriteLine("testf5");
        }
    }
}
