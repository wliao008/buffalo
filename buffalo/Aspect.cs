namespace Buffalo
{
    public abstract class Aspect : System.Attribute
    {
        public virtual void OnEntry() { }

        public virtual void OnExit() { }
    }
}
