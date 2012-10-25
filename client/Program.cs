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
            test.Message = "This is a property";
            var user = test.GetUser();
            Console.WriteLine(user.Username);
            Console.WriteLine(test.Message);
            Console.WriteLine("DONE");
            Console.Read();
        }
    }

    [TraceAspect]
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
    }

    public class User
    {
        public string Username { get; set; }
    }
}
