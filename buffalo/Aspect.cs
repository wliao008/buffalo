namespace Buffalo
{
    public abstract class Aspect : System.Attribute, IAspect
    {
        public Aspect(bool attributeExclude = false)
        {
            this.AttributeExclude = attributeExclude;
        }

        public bool AttributeExclude { get; set; }

        public virtual void Before() { }

        public virtual void After() { }

        public virtual void Around() { }

        public virtual void Exception() { }
    }
}
