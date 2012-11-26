using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffalo;
using System.Threading;
using ClientAppLib.Aspects;

namespace client
{
    /// <summary>
    /// post build event: "$(TargetDir)BuffaloAOP.exe" "$(TargetPath)"
    /// </summary>
    class Program
    {
        static AspectTester test = new AspectTester();
        static void Main(string[] args)
        {
            var result = test.divide(6, 3);
            Console.WriteLine("result: " + result);

            test.OpenFile();

            Console.WriteLine("DONE");
            Console.Read();
        }
    }

    public class AspectTester
    {
        [AroundAspect2]
        public decimal divide(int a, int b)
        {
            return a / b;
        }

        public void OpenFile()
        {
            var t = System.IO.File.ReadAllText("c:\\abc.txt");
            Console.WriteLine(t);
        }
    }
}
