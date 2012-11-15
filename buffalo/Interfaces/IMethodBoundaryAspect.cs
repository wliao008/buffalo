namespace Buffalo.Interfaces
{
    internal interface IMethodBoundaryAspect : IAspect
    {
        void OnBefore(MethodArgs args);
        void OnAfter(MethodArgs args);
        void OnSuccess(MethodArgs args);
        void OnException(MethodArgs args);
    }
}
