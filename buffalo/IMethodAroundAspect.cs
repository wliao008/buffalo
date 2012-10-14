
namespace Buffalo
{
    internal interface IMethodAroundAspect : IAspect
    {
        void Invoke(MethodDetail detail);
    }
}
