using Buffalo.Interfaces;

namespace Buffalo
{
    public abstract class MethodBoundaryAspect : AspectBase, IMethodBoundaryAspect
    {
        public virtual void OnBefore(MethodArgs args) { }

        public virtual void OnAfter(MethodArgs args) { }

        public virtual void OnSuccess(MethodArgs args) { }

        public virtual void OnException(MethodArgs args) { }
    }
}
