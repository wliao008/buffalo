using Buffalo;
using System;

namespace client
{
    [MyAspect(AttributeExclude=true)]
    public class MyAspect : MethodBoundaryAspect
    {
        public override void Before()
        {
            Console.WriteLine("Entering...");
        }
    }
}
