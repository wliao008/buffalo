using System;
namespace Buffalo
{
    public abstract class MethodBoundaryAspect : System.Attribute, IMethodBoundaryAspect
    {
        public MethodBoundaryAspect()
        {
            this.AttributeExclude = false;
        }

        public MethodBoundaryAspect(bool attributeExclude = false)
        {
            this.AttributeExclude = attributeExclude;
        }

        public bool AttributeExclude { get; set; }

        public virtual void Before(string name, string fullname) { }

        public virtual void After(string name, string fullname) { }

        public virtual void Success(string name, string fullname) { }

        public virtual void Exception(string name, string fullname) { }
    }
}
