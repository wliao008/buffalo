namespace Buffalo
{
    public abstract class Aspect : System.Attribute, IAspect
    {
        public virtual void Before() { }

        public virtual void After() { }

        public virtual void Around() { }

        public virtual void Exception() { }
    }
}
