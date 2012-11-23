using Buffalo.Interfaces;

namespace Buffalo
{
    public abstract class MethodAroundAspect : AspectBase, IMethodAroundAspect
    {
        public virtual object Invoke(MethodArgs args) { return null; }
    }
}
