
namespace Buffalo
{
    internal interface IMethodAroundAspect : IAspect
    {
        object Invoke(MethodArgs args);
    }
}
