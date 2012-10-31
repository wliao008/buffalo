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

            var result = test.Add(2, 6);
            Console.WriteLine("result: " + result);

            Console.WriteLine("DONE");
            Console.Read();
        }
    }

    public class TraceAspectTester
    {
        [DoubleAspect]
        public int Add(int a, int b)
        {
            int c = a + b;
            return c;
        }
    }
}
