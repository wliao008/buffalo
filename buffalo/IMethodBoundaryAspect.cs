namespace BuffaloAOP
{
    internal interface IMethodBoundaryAspect : IAspect
    {
        void Before(MethodDetail detail);
        void After();
        void Success();
        void Exception();
    }
}
