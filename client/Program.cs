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

        [MyAspect(AttributeExclude = true)]
        public void Function1()
        {
            Console.WriteLine("Function 1");
        }

        public void Function2()
        {
            Console.WriteLine("Function 2");
        }
    }
}
