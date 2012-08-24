using Buffalo;
using System;

namespace client
{
    [MyAspect(AttributeExclude = true)]
    [TraceAspect(AttributeExclude = true)]
    public class MyAspect : MethodBoundaryAspect
    {
        public override void Before()
        {
            Console.WriteLine("MyAspect.Before");
        }

        public override void After()
        {
            Console.WriteLine("MyAspect.After");
        }

        public override void Success()
        {
            Console.WriteLine("MyAspect.Success");
        }
    }

    [MyAspect(AttributeExclude = true)]
    [TraceAspect(AttributeExclude = true)]
    public class TraceAspect : MethodBoundaryAspect
    {
        public override void Before()
        {
            Console.WriteLine("Trace.Before");
        }
    }
}
