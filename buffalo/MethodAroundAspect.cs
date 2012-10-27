
namespace Buffalo
{
    public abstract class MethodAroundAspect : System.Attribute, IMethodAroundAspect
    {
        public MethodAroundAspect(bool attributeExclude = false)
        {
            this.AttributeExclude = attributeExclude;
        }

        public bool AttributeExclude { get; set; }

        public virtual void Invoke(MethodArgs args) { }
    }
}
