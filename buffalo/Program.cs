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
            new Ass();
            Console.WriteLine("DONE");
            Console.Read();
        }
    }

    class Ass
    {
        string path = @"C:\Users\wliao\Documents\Visual Studio 2010\Projects\buffalo\client\bin\Debug\client.exe";
        public Ass()
        {
            Assembly assembly = Assembly.LoadFrom(path);
            var asses = assembly.GetCustomAttributes(typeof(Aspect), false);
            Console.WriteLine("\n\nASS-------------------");
            foreach (var ass in asses)
            {
                Console.WriteLine(ass.ToString());
            }

            var types = assembly.GetTypes().ToList();
            Console.WriteLine("\n\nTYPES-------------------");
            foreach (var t in types)
            {
                Console.WriteLine(t.Name);
                var cs = t.GetCustomAttributes(typeof(Aspect), false);
                foreach (var c in cs)
                {
                    Console.WriteLine("\t" + c.ToString());
                }
            }

            var methods = types
                          .SelectMany(t => t.GetMethods())
                          .Where(m => m.GetCustomAttributes(typeof(Aspect), false).Length > 0)
                          .ToArray();

            Console.WriteLine("\n\nMETHODS-------------------");
            foreach (var m in methods)
            {
                Console.WriteLine(m.Name);
                var attrs = m.GetCustomAttributesData();
                foreach (var a in attrs)
                {
                    Console.WriteLine("\t" + a.ToString());
                    foreach (var arg in a.NamedArguments)
                    {
                        Console.WriteLine("\t" + arg.MemberInfo.Name + ", " + arg.TypedValue);
                    }
                }
            }
        }
    }
}
