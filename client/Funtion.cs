using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffalo;

namespace client
{
    public class Funtion
    {
        [MyAspect(AttributeExclude = true)]
        public void FFunction1()
        {
            Console.WriteLine("Funtion.Function1");
        }

        [TraceAspect]
        public void Function2a()
        {
            Console.WriteLine("Funtion.Function2");
        }

        [MyAspect(AttributeExclude = true)]
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
