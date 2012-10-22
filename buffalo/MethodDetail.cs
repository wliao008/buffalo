using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Buffalo
{
    public sealed class MethodDetail
    {
        private string name;
        private List<Parameter> parameters;

        public MethodDetail()
        {
            //this.Init();
        }

        public void Proceed() { }

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

        public void setName(string name)
        {
            this.name = name;
        }

        private void Init()
        {
            ///TODO: StackTrace is only available in DEBUG!
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
