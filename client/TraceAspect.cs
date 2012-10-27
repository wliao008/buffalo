using System;
using System.Diagnostics;
using System.Threading;
using Buffalo;

namespace client
{
    [TraceAspect(AttributeExclude = true)]
    [Trace2Aspect(AttributeExclude = true)]
    [MyAroundAspect(AttributeExclude = true)]
    public class TraceAspect : MethodBoundaryAspect
    {
        Random r = new Random();
        public override void Before(string name, string fullname)
        {
            Console.WriteLine("Trace.Before " + r.Next(1,99));
            this.Display(name, fullname);
        }

        //public override void Before(MethodDetail detail)
        //{
        //    Console.WriteLine("Trace.Before: " + detail.Name);
        //}

        public override void Exception(string name, string fullname)
        {
            Console.WriteLine("********* TRACE EXCEPTION!! ********:");
            this.Display(name, fullname);
        }

        public override void Success(string name, string fullname)
        {
            Console.WriteLine("Trace.Success:" + r.Next(200, 299));
            this.Display(name, fullname);
        }

        public override void After(string name, string fullname)
        {
            Console.WriteLine("Trace.After: " + r.Next(300, 399));
            this.Display(name, fullname);
        }

        private void Display(string name, string fullname)
        {
            Console.WriteLine("\tat: {0}\n\t{1}\n", name, fullname);
        }
    }


    [TraceAspect(AttributeExclude = true)]
    [Trace2Aspect(AttributeExclude = true)]
    [MyAroundAspect(AttributeExclude = true)]
    public class MyAroundAspect : MethodAroundAspect
    {
        public override void Invoke(MethodDetail detail)
        {
            var usr = GetFakeUser();
            Console.WriteLine("Fake user: " + usr.Username);
        }

        public User GetFakeUser()
        {
            return new User { Username = "Fake username" };
        }
    }

    [TraceAspect(AttributeExclude = true)]
    [Trace2Aspect(AttributeExclude = true)]
    [MyAroundAspect(AttributeExclude = true)]
    public class Trace2Aspect : MethodBoundaryAspect
    {
        public override void Before(string name, string fullname)
        {
            Console.WriteLine("Trace2.Before");
            this.Display(name, fullname);
        }

        public override void Exception(string name, string fullname)
        {
            Console.WriteLine("********* TRACE2 EXCEPTION!! ********:");
            this.Display(name, fullname);
        }

        public override void Success(string name, string fullname)
        {
            Console.WriteLine("Trace2.Success");
            this.Display(name, fullname);
        }

        public override void After(string name, string fullname)
        {
            Console.WriteLine("Trace2.After");
            this.Display(name, fullname);
        }

        private void Display(string name, string fullname)
        {
            Console.WriteLine("\tat: {0}\n\t{1}\n", name, fullname);
        }
    }
}
