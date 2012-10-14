using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Buffalo
{
    public class MethodDetail
    {
        private string name;
        private List<Parameter> parameters;

        public MethodDetail()
        {
            this.Init();
        }

        public List<Parameter> Parameters
        {
            get
            {
                return this.parameters;
            }
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        private void Init()
        {
            StackTrace trace = new StackTrace();
            Console.WriteLine("frames: " + trace.FrameCount);
            var method = trace.GetFrame(2).GetMethod();
            var param = method.GetParameters();
            this.name = method.Name;
            if (this.parameters == null)
            {
                this.parameters = new List<Parameter>();
            }
            foreach (var p in param)
            {
                this.parameters.Add(new Parameter { Name = p.Name, Type = p.ParameterType });
            }
        }
    }

    public class Parameter
    {
        public string Name { get; set; }

        public Type Type { get; set; }
    }
}
