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

            var result = test.divide(6, 3);
            Console.WriteLine("result: " + result);

            int x = 10;
            Func<int, int> foo = y => y + x;
            Console.WriteLine("y: " + foo(x));

            List<int> arr = new List<int>() { 1, 2, 3 };
            int three = arr.FirstOrDefault(z => z == 3);
            Console.WriteLine("Found: " + three);
            Console.WriteLine("DONE");
            Console.Read();
        }
    }

    public class TraceAspectTester
    {
        //[DoubleAspect]
        public double divide(int a, int b)
        {
            //Console.WriteLine(a);
            //Console.WriteLine(b);
            return a / b;
        }
    }
}
