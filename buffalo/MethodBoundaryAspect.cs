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

        public virtual void Before(MethodArgs args) { }

        public virtual void After(MethodArgs args) { }

        public virtual void Success(MethodArgs args) { }

        public virtual void Exception(MethodArgs args) { }
    }
}
