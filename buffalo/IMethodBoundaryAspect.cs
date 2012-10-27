using System;
namespace Buffalo
{
    internal interface IMethodBoundaryAspect : IAspect
    {
        void Before(MethodArgs args);
        void After(string name, string fullname);
        void Success(string name, string fullname);
        void Exception(string name, string fullname);
    }
}
