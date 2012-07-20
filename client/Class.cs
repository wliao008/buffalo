using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace client
{
    [MyAspect]
    public class Class
    {
        public void Function1()
        {
            Console.WriteLine("Class.Function1");
        }

        public void Function2()
        {
            Console.WriteLine("Class.Function2");
        }

        public void Function3()
        {
            Console.WriteLine("Class.Function3");
        }

        public void Function4()
        {
            Console.WriteLine("Class.Function1");
        }

        [DummyAttribute]
        public void Function5()
        {
            Console.WriteLine("Class.Function1");
        }
    }

    [DummyAttribute]
    public class DummyClass
    {
    }

    public class DummyAttribute : Attribute
    {
    }
}
