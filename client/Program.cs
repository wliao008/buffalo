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
        static Test test = new Test();
        static void Main(string[] args)
        {
            Console.WriteLine("Program.Main");
            test.Function1(1);
            //var result = test.Add(10, 2);
            //Console.WriteLine("result: " + result);

            var usr = test.GetUser();
            Console.WriteLine("User: " + usr.Firstname);
            Console.Read();
        }
    }

    public class Test
    {
        public Test()
        {
            Console.WriteLine("Test.ctor");
        }

        [TraceAspect]
        public void Function1(int a)
        {
            Console.WriteLine("Function 1");
            this.Divide();
            this.DummyString("my", "name");
        }

        public void Function1b(int b)
        {
            try
            {
                MethodDetail m = new MethodDetail();
                m.setName("hey");
                this.DoMethodDetail(m);
                Console.WriteLine("Function 1");
                this.Divide();
            }
            catch (Exception e)
            {
                Console.Write(e);
            }
        }

        [TraceAspect]
        public int Add(int a, int b)
        {
            return a + b;
        }

        [TraceAspect]
        public void TestMethodDetail(int a)
        {
            MethodDetail m = new MethodDetail();
            m.setName("hey");
        }

        public void Divide()
        {
            int z = 0;
            int c = 1 / z;
        }

        public double Function2(int num1, int num2)
        {
            int a = 1;
            double result = num1 / num2;
            Console.WriteLine("blah blah...");
            return result;
        }

        [TraceAspect]
        public int DummyException(int num1, int num2, User us)
        {
            int c = 2;
            int d = 8;
            User u = us;
            try
            {
                int result = num1 + num2;
                Console.WriteLine("blah blah...");
                int a = 1;
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return -1;
            }
        }

        public void DummyString(string str1, string str2)
        {
            Console.WriteLine(str1 + ": " + str2);
        }

        [TraceAspect]
        public User GetUser()
        {
            User u = new User { Firstname = "Wei" };
            return u;
        }

        public void DoMethodDetail(MethodDetail detail)
        {
        }
    }

    public class User
    {
        public string Firstname { get; set; }
    }
}
