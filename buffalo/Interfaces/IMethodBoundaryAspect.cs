namespace Buffalo.Interfaces
{
    internal interface IMethodBoundaryAspect : IAspect
    {
        void Before(MethodArgs args);
        void After(MethodArgs args);
        void Success(MethodArgs args);
        void Exception(MethodArgs args);
    }
}
