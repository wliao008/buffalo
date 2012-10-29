using System;
using System.Collections.Generic;

namespace Buffalo
{
    public sealed class MethodArgs
    {
        private string name;
        private string fullName;
        private string returnTypeStr;
        private string parameterStr;
        private List<Parameter> parameters;
        private Exception exception;

        public MethodArgs()
        {
            this.parameters = new List<Parameter>();
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        public string FullName
        {
            get
            {
                return this.fullName;
            }
        }

        public Type ReturnType
        {
            get
            {
                return Type.GetType(this.returnTypeStr);
            }
        }

        public List<Parameter> Parameters
        {
            get { return this.parameters; }
        }

        public Exception Exception
        {
            get { return this.exception; }
        }

        public void SetProperties(string name, 
            string fullname, 
            string returnTypeStr,
            string parameterStr)
        {
            this.name = name;
            this.fullName = fullname;
            this.returnTypeStr = returnTypeStr;
            this.parameterStr = parameterStr;

            var splits = this.parameterStr.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var split in splits)
            {
                var p = split.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                this.parameters.Add(new Parameter { Name = p[0], Type = Type.GetType(p[1]) });
            }
        }

        public void SetException(Exception exception)
        {
            this.exception = exception;
        }

        public object Proceed()
        {
            return null;
        }
    }
}
