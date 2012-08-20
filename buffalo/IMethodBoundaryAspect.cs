namespace Buffalo
{
    public interface IMethodBoundaryAspect : IAspect
    {
        void Before();
        void After();
        void Around();
        void Exception();
    }
}
