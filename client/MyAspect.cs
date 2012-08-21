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

    [TraceAspect(AttributeExclude = true)]
    public class TraceAspect : MethodBoundaryAspect
    {
        public override void Before()
        {
            Console.WriteLine("Dude, this is before");
        }
    }
}
