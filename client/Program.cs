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
            //Int16 i = 1;
            //object o = i;
            //double d = (double)(Int16)o;
            //Console.WriteLine("d: " + d);

            var result = test.divide(6, 3);
            Console.WriteLine("result: " + result);

            test.OpenFile();

            Console.WriteLine("DONE");
            Console.Read();
        }
    }

    public class TraceAspectTester
    {
        [DoubleAspect]
        public decimal divide(int a, int b)
        {
            //Console.WriteLine(a);
            //Console.WriteLine(b);
            return a / b;
        }

        public void OpenFile()
        {
            var t = System.IO.File.ReadAllText("c:\\abc.txt");
            Console.WriteLine(t);
        }
    }
}
