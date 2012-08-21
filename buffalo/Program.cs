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
            string outPath = @"C:\Users\wliao\Documents\Visual Studio 2010\Projects\buffalo\client\bin\Debug\client_modified.exe";
            string path = @"C:\Users\wliao\Documents\Visual Studio 2010\Projects\buffalo\client\bin\Debug\client.exe";
            //string path = @"C:\Users\wliao\Documents\Visual Studio 2010\Projects\FMS\DEV\Trunk\Source\FMS\Client\WpfClient\WpfClient\bin\Debug\SDX.FMS.SharedServices.dll";
            /*
            var methods = new Weaver(path).GetAllMethods();
            Console.WriteLine("\n\n======================== methods ========================");
            foreach (var m in methods)
            {
                Console.WriteLine(m.Name);
            }
            */

            new Weaver(path)
                .Inject(outPath);
            Console.Read();
        }
    }
}
