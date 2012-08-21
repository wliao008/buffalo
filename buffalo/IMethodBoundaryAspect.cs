namespace Buffalo
{
    internal interface IMethodBoundaryAspect : IAspect
    {
        void Before();
        void After();
        void Around();
        void Exception();
    }
}
