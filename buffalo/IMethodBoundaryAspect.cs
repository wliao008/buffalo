using System;
namespace Buffalo
{
    internal interface IMethodBoundaryAspect : IAspect
    {
        void Before(string name, string fullname);
        void After(string name, string fullname);
        void Success(string name, string fullname);
        void Exception(string name, string fullname);
    }
}
