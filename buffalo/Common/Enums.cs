namespace Buffalo.Common
{
    internal class Enums
    {
        internal enum Status
        {
            Applied,
            NotApplied,
            Excluded
        }

        internal enum AspectType
        {
            OnBefore,
            OnAfter,
            OnSuccess,
            OnException,
            Invoke
        }

        internal enum BuffaloAspect
        {
            MethodBoundaryAspect,
            MethodAroundAspect
        }
    }
}
