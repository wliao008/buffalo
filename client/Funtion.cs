﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffalo;

namespace client
{
    public class Funtion
    {
        [MyAspect(AttributeExclude=true)]
        public void Function1()
        {
            Console.WriteLine("Funtion.Function1");
        }

        public void Function2()
        {
            Console.WriteLine("Funtion.Function2");
        }

        [MyAspect]
        public void Function3()
        {
            Console.WriteLine("Funtion.Function3");
        }

        public void Function4()
        {
            Console.WriteLine("Funtion.Function1");
        }

        public void Function5()
        {
            Console.WriteLine("Funtion.Function1");
        }
    }
}
