using System;
using System.Diagnostics;
using System.Threading;
using Buffalo;

namespace client
{
    [TraceAspect(AttributeExclude = true)]
    [MyAroundAspect(AttributeExclude = true)]
    [FakeAddAspect(AttributeExclude = true)]
    public class TraceAspect : MethodBoundaryAspect
    {
        Random r = new Random();
        public override void Before(MethodArgs args)
        {
            Console.WriteLine("Trace.Before " + r.Next(1, 99));
            this.Display(args);
        }

        public override void Exception(MethodArgs args)
        {
            Console.WriteLine("********* TRACE EXCEPTION!! ********:" + r.Next(400,499));
            this.Display(args);
        }

        public override void Success(MethodArgs args)
        {
            Console.WriteLine("Trace.Success:" + r.Next(200, 299));
            this.Display(args);
        }

        public override void After(MethodArgs args)
        {
            Console.WriteLine("Trace.After: " + r.Next(300, 399));
            this.Display(args);
        }

        private void Display(MethodArgs args)
        {
            Console.WriteLine("\tReturnType: " + args.ReturnType.BaseType.FullName);
            Console.WriteLine("\tName: " + args.Name);
            Console.WriteLine("\tFull Name: " + args.FullName);
            Console.WriteLine("\tParameters: ");
            foreach (var p in args.Parameters)
            {
                Console.WriteLine("\t\t{0}: {1}", p.Name, p.Type);
            }
            if (args.Exception != null)
            {
                Console.WriteLine("\tException: " + args.Exception.Message);
            }
        }
    }

    [TraceAspect(AttributeExclude = true)]
    [MyAroundAspect(AttributeExclude = true)]
    [FakeAddAspect(AttributeExclude = true)]
    public class MyAroundAspect : MethodAroundAspect
    {
        public override object Invoke(MethodArgs args)
        {
            if (10 % 2 == 0)
            {
                object obj = args.Proceed();
                return obj;
            }
            else
            {
                var usr = GetFakeUser();
                Console.WriteLine("Fake user: " + usr.Username);
                return null;
            }
        }

        public User GetFakeUser()
        {
            return new User { Username = "Fake username" };
        }
    }
    
    [TraceAspect(AttributeExclude = true)]
    [MyAroundAspect(AttributeExclude = true)]
    [FakeAddAspect(AttributeExclude = true)]
    public class FakeAddAspect : MethodAroundAspect
    {
        Random r = new Random((int)DateTime.Now.Ticks);
        public override object Invoke(MethodArgs args)
        {
            //var num = r.Next(1, 100);
            //if(num % 2 == 0){
            //    return args.Proceed();
            //}
            var result = 2;
            Console.WriteLine("in fake aspect: " + result);
            return result;
        }
    }

    /*
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
    */
}
