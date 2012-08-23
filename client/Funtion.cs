using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buffalo;

namespace client
{
    public class Funtion
    {
        public int Id { get; set; }

        [MyAspect(AttributeExclude = true)]
        public void FFunction1()
        {
            Console.WriteLine("Funtion.Function1");
        }

        [TraceAspect]
        [MyAspect(AttributeExclude = true)]
        public void Function2a()
        {
            Console.WriteLine("Funtion.Function2a");
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
