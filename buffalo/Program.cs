﻿using System;
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
            string outPath = @"C:\Users\wliao\Documents\Visual Studio 2012\Projects\buffalo\client\bin\Debug\client_modified.exe";
            string path = @"C:\Users\wliao\Documents\Visual Studio 2012\Projects\buffalo\client\bin\Debug\client.exe";
            new Weaver(path).Inject(outPath);
            Console.Read();
        }
    }
}
