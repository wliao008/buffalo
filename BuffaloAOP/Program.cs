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
            if (args == null || args.Count() == 0)
            {
                Console.WriteLine("USAGE: BuffaloAOP.exe <assembly_path>");
                Environment.Exit(1);
            }

            path = args[0];
            path = @"C:\Users\Wei Liao\Documents\Visual Studio 2012\Projects\Tests\TransactionalDb\TransactionalDb\TransactionalDb\bin\Debug\TransactionalDb.exe";
            new Weaver(path).Inject();
        }
    }
}
