using System;
using System.Linq;
using System.Reflection;

namespace Buffalo
{
    public class Weaver
    {
        private string assemblyPath;

        public Weaver(string assemblyPath)
        {
            ///TODO: check for file existant
            this.assemblyPath = assemblyPath;
        }

        public MethodInfo[] GetAllMethods()
        {
            Assembly assembly = Assembly.LoadFrom(this.assemblyPath);
            var namespaces = assembly.GetCustomAttributes(typeof(Aspect), false);
            Console.WriteLine("NAMESPACES--------------------: " + namespaces.Count());
            foreach (var n in namespaces)
            {
                Console.WriteLine(n.ToString());
            }

            var types = assembly.GetTypes().ToList();
            Console.WriteLine("\n\nTYPES-------------------: " + types.Count);
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

            Console.WriteLine("\n\nMETHODS-------------------: " + methods.Count());
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

            return null;
        }
    }
}
