namespace Buffalo
{
    public abstract class MethodBoundaryAspect : System.Attribute, IMethodBoundaryAspect
    {
        public MethodBoundaryAspect(bool attributeExclude = false)
        {
            this.AttributeExclude = attributeExclude;
        }

        public bool AttributeExclude { get; set; }

        public virtual void Before() { }

        public virtual void After() { }

        public virtual void Success() { }

        public virtual void Exception() { }
    }
}
