using System;
namespace Buffalo
{
    internal interface IMethodBoundaryAspect : IAspect
    {
        void Before(string name, string fullname);
        void After();
        void Success();
        void Exception();
    }
}
