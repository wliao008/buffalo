using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Buffalo
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"C:\Users\wliao\Documents\Visual Studio 2010\Projects\buffalo\client\bin\Debug\client.exe";
            var methods = new Weaver(path).GetAllMethods();
            Console.WriteLine("\n\n======================== methods ========================");
            foreach (var m in methods)
            {
                Console.WriteLine(m.Name);
            }
            Console.Read();
        }
    }
}
