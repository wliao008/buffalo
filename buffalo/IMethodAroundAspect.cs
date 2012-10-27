
namespace Buffalo
{
    internal interface IMethodAroundAspect : IAspect
    {
        void Invoke(MethodArgs args);
    }
}
