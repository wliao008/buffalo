using Buffalo;
using System;

namespace client
{
    public class MyAspect : Aspect
    {
        public override void Before()
        {
            Console.WriteLine("Entering...");
        }
    }
}
