using Buffalo;
using System;

namespace client
{
    [MyAspect(AttributeExclude=true)]
    public class MyAspect : Aspect
    {
        public override void Before()
        {
            Console.WriteLine("Entering...");
        }
    }
}
