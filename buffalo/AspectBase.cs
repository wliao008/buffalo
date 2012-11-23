namespace Buffalo
{
    [System.AttributeUsage(System.AttributeTargets.All,
        AllowMultiple = false)]
    public abstract class AspectBase : System.Attribute
    {
        public AspectBase(bool attributeExclude = false)
        {
            this.AttributeExclude = attributeExclude;
        }

        public bool AttributeExclude { get; set; }
    }
}
