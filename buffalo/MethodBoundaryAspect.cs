namespace Buffalo
{
    public abstract class MethodBoundaryAspect : System.Attribute, IMethodBoundaryAspect
    {
        public MethodBoundaryAspect(bool attributeExclude = false)
        {
            this.AttributeExclude = attributeExclude;
        }

        public bool AttributeExclude { get; set; }

        public virtual void Before(MethodDetail detail) { }

        public virtual void After(MethodDetail detail) { }

        public virtual void Success() { }

        public virtual void Exception(MethodDetail detail) { }
    }
}
