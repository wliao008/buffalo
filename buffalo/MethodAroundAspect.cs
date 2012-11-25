using Buffalo.Interfaces;

namespace Buffalo
{
    [System.AttributeUsage(System.AttributeTargets.Method,
        AllowMultiple = false)]
    public abstract class MethodAroundAspect : AspectBase, IMethodAroundAspect
    {
        public virtual object Invoke(MethodArgs args) { return null; }
    }
}
