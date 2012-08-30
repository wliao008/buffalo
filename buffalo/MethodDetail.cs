using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Buffalo
{
    public class MethodDetail
    {
        private string name;
        private Dictionary<string, object> parameters;

        public MethodDetail()
        {
            this.Init();
        }

        public Dictionary<string, object> Parameters
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
                this.parameters = new Dictionary<string, object>();
            }
            foreach (var p in param)
            {
                this.parameters.Add(p.Name, p.Name);
            }
        }
    }
}
