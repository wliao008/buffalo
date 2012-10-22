using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffalo;
using System.Threading;

namespace client
{
    /// <summary>
    /// post build event: "$(TargetDir)BuffaloAOP.exe" "$(TargetPath)"
    /// </summary>
    class Program
    {
        static Test test = new Test();
        static void Main(string[] args)
        {
            Console.WriteLine("Program.Main");
            //test.TestF1();
            //test.TestF2();
            //test.Add(1, 2);
            test.Function1(1);
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

        //[MyAroundAspect]
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

        public void TestF6()
        {
            Console.WriteLine("TestF6...");
            var ticks = DateTime.Now.Ticks;
            var call = ticks % 2;
            Console.WriteLine("ticks: " + ticks);
            if (ticks == 0)
            {
                TestF7();
            }
            else
            {
                Console.WriteLine("can't call since it's odd");
            }
        }

        public void TestF7()
        {
            Console.WriteLine("testf7");
        }

        [MyFakeAddAspect]
        public void Add(int a, int b)
        {
            Console.WriteLine(a + b);
        }
    }

    public class MyDummyTest
    {
        public void Function1()
        {
            //stupid test
            int a = 0;
            Console.WriteLine("a = " + a);
        }

        public void Function2()
        {
            MethodDetail det = new MethodDetail();
            det.setName("Function2()");
            Function3(det);
        }

        public void Function3(MethodDetail detail)
        {
        }
    }
}
