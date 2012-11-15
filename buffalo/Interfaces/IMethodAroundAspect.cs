namespace Buffalo.Interfaces
{
    internal interface IMethodAroundAspect : IAspect
    {
        object Invoke(MethodArgs args);
    }
}
