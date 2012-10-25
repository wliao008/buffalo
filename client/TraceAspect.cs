using System;
using System.Diagnostics;
using System.Threading;
using Buffalo;

namespace client
{
    [TraceAspect(AttributeExclude = true)]
    public class TraceAspect : MethodBoundaryAspect
    {
        public override void Before(string name, string fullname)
        {
            Console.WriteLine("Trace.Before");
            this.Display(name, fullname);
        }

        //public override void Before(MethodDetail detail)
        //{
        //    Console.WriteLine("Trace.Before: " + detail.Name);
        //}

        public override void Exception(string name, string fullname)
        {
            Console.WriteLine("********* TRACE EXCEPTION!! ********");
            this.Display(name, fullname);
        }

        public override void Success(string name, string fullname)
        {
            Console.WriteLine("Trace.Success");
            this.Display(name, fullname);
        }

        public override void After(string name, string fullname)
        {
            Console.WriteLine("Trace.After");
            this.Display(name, fullname);
        }

        private void Display(string name, string fullname)
        {
            Console.WriteLine("\tat: {0}\n\t{1}\n", name, fullname);
        }
    }
}
