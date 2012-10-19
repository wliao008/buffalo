using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Buffalo;

namespace BuffaloAOP
{
    class Program
    {
        static string path;
        static void Main(string[] args)
        {
            //path = @"C:\Users\Wei.Liao\Documents\Visual Studio 2012\Projects\buffalo\client\bin\Debug\client.exe";
            //string outpath = @"C:\Users\Wei.Liao\Documents\Visual Studio 2012\Projects\buffalo\client\bin\Debug\client_modified.exe";

            path = args[0];
            string outpath = path.Replace(".exe", "_modified.exe").Replace(".dll", "_modified.dll");
            new Weaver(path).Inject(outpath);
            Console.Read();
        }
    }
}
