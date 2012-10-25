using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffalo;
using System.Threading;

namespace client
{
    /// <summary>
    /// post build event: "$(TargetDir)BuffaloAOP.exe" "$(TargetPath)"
    /// </summary>
    class Program
    {
        static TraceAspectTester test = new TraceAspectTester();
        static void Main(string[] args)
        {
            test.RealNumNoArgs();
            //Console.WriteLine("Real result: " + result);


            Console.WriteLine("DONE");
            Console.Read();
        }
    }

    //[TraceAspect]
    public class TraceAspectTester
    {
        private string _message;

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public void TestDivideByZero()
        {
            int z = 0;
            int c = 1 / z;
            Console.WriteLine(c);
        }

        public void TestDivideByZeroWithTryCatch()
        {
            try
            {
                int z = 0;
                int c = 1 / z;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public User GetUser()
        {
            return new User { Username = "weiliao" };
        }

        [MyAroundAspect]
        public void RealNum(int a, int b)
        {
            //int a = 10;
            //int b = 40;
            int c = a + b;
            Console.WriteLine("RealNum: " + c);
        }

        [MyAroundAspect]
        public void RealNumNoArgs()
        {
            int a = 10;
            int b = 40;
            int c = a + b;
            Console.WriteLine("RealNum: " + c);
        }

        public int Add(int a, int b)
        {
            int c = a + b;
            return c;
        }

        public int Divide(int a, int b)
        {
            int c = a / b;
            return c;
        }

        public int DivideWithTryCatch(int a, int b)
        {
            int c = 0;
            try
            {
                c = a / b;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return c;
        }

        public void TestOut(out int age)
        {
            Console.WriteLine("testing out");
            age = 12;
        }
    }

    public class User
    {
        public string Username { get; set; }
    }
}
