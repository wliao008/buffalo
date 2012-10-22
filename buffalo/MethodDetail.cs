using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Buffalo
{
    public sealed class MethodDetail
    {
        private string name;
        private Exception exception;
        private List<Parameter> parameters;

        public MethodDetail()
        {
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

        public Exception Exception
        {
            get
            {
                return this.exception;
            }
        }

        public void setName(string name)
        {
            this.name = name;
        }

        public void setException(Exception e)
        {
            this.exception = e;
        }
    }

    public class Parameter
    {
        public string Name { get; set; }

        public Type Type { get; set; }
    }
}
