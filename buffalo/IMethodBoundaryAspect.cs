namespace Buffalo
{
    internal interface IMethodBoundaryAspect : IAspect
    {
        void Before();
        void After();
        void Success();
        void Exception();
    }
}
