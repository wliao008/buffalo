namespace Buffalo
{
    internal interface IMethodBoundaryAspect : IAspect
    {
        void Before(MethodDetail detail);
        void After(MethodDetail detail);
        void Success();
        void Exception();
    }
}
