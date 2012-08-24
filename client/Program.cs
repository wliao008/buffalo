using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffalo;

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

        [TraceAspect(AttributeExclude = true)]
        [MyAspect]
        public void Function1()
        {
            int zero = 0;
            //int result = 1 / zero;
            Console.WriteLine("Function 1");
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
    }
}
