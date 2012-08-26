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
            new Test().Function1();
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
        public void Function1()
        {
            int zero = 0;
            //int result = 1 / zero;
            Thread.Sleep(500);
            Console.WriteLine("Function 1");
            this.Function3();
        }

        public void Function2()
        {
            try
            {
                int zero = 0;
                //int result = 1 / zero;
                Console.WriteLine("Function 2");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.WriteLine("finally");
            }
        }

        public void Function3()
        {
            Console.WriteLine("Function 3");
        }
    }
}
