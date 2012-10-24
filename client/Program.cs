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
            test.Function1(1);
            var result = test.Add(10, 2);
            Console.WriteLine("result: " + result);
            Console.Read();
        }
    }

    public class Test
    {
        public Test()
        {
            Console.WriteLine("Test.ctor");
        }

        [TraceAspect]
        public void Function1(int a)
        {
            Console.WriteLine("Function 1");
            this.Divide();
        }

        [TraceAspect]
        public int Add(int a, int b)
        {
            return a + b;
        }

        public void Divide()
        {
            int z = 0;
            int c = 1 / z;
        }

        public double Function2(int num1, int num2)
        {
            double result = num1 / num2;
            Console.WriteLine("blah blah...");
            int a = 1;
            return result;
        }

        public int DummyException(int num1, int num2)
        {
            try
            {
                int result = num1 + num2;
                Console.WriteLine("blah blah...");
                int a = 1;
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return -1;
            }
        }
    }
}
