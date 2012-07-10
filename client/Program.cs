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
        }
    }

    class MyAspect : Aspect
    {
        public override void OnEntry()
        {
            Console.WriteLine("Entering...");
        }
    }
}
