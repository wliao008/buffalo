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

            //object[] objs = new object[2];
            //objs[0] = 10;
            //objs[1] = 11;
            //TestArgs t = new TestArgs();
            //DummyArgs arg = new DummyArgs();
            //arg.SetArgs(objs);
            //t.Invoke(arg);

            var result = test.Add(2, 76);
            Console.WriteLine("result: " + result);

            Console.WriteLine("DONE");
            Console.Read();
        }
    }

    public class TraceAspectTester
    {
        private string _message;

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        //[MyAroundAspect]
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
            return new User { Username = "weiliao", Age = 33 };
        }

        [TraceAspect]
        public void SetUser(User usr)
        {
            User u = usr;
            Console.WriteLine("{0}: {1}", u.Age, u.Username);
        }

        public User GetUserNoAround(User usr)
        {
            return usr;
        }

        //[MyAroundAspect]
        //[TraceAspect]
        [TraceAspect]
        public void RealNum(int a, short b, bool processed)
        {
            //int a = 10;
            //int b = 40;
            int c = a + b;
            Console.WriteLine("RealNum: " + c);
        }

        [TraceAspect]
        public void RealNumNoArgs()
        {
            int a = 10;
            int b = 40;
            int c = a + b;
            Console.WriteLine("RealNum: " + c);
        }


        //[TraceAspect]
        [FakeAddAspect]
        public int Add(int a, int b)
        {
            //TraceAspect ta = new TraceAspect();
            //ta.Before("Add", "wei_Add");

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
        public int Age { get; set; }

        //public override string ToString()
        //{
        //    return string.Format("{0}: {1}", Age, Username);
        //}
    }

    public class Dummy
    {
        public object Add(int a, int b)
        {
            return a + b;
        }

        public void HeyDummy()
        {
            int result = (int)Add(10, 2);
            Console.WriteLine("result: " + result);
        }

        public void TestObjArray()
        {
            object[] objs = new object[5];
            objs[0] = 0;
            objs[1] = 1;
            objs[2] = 2;
            objs[3] = 3;
            objs[4] = 4;

            for(int i=0;i<5;i++)
                Console.WriteLine(objs[i]);
        }
    }

    public class TestArgs
    {
        public void Invoke(DummyArgs args)
        {
            Worker w = new Worker();
            object[] objs = args.Args;
            var result = w.Add((int)objs[0], (int)objs[1]);
            Console.WriteLine(result);
        }
    }

    public class Worker
    {
        public int Add(int a, int b)
        {
            return a + b;
        }
    }

    public class DummyArgs
    {
        object[] args;
        public object[] Args
        {
            get { return args; }
        }
        public void SetArgs(object[] args)
        {
            this.args = args;
        }
    }

    public class DummyParam
    {
    }
}
