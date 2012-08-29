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
        MethodDetail md = new MethodDetail();
        public Test()
        {
            Console.WriteLine("Test.ctor");
        }

        [TraceAspect(AttributeExclude=true)]
        [MyAspect]
        public void Function1()
        {
            Console.WriteLine("Function 1");
            this.Function3();
        }

        public void Function2()
        {
            try
            {
                //int result = 1 / zero;
                Console.WriteLine(md);
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
