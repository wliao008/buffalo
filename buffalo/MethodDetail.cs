using System;
using System.Diagnostics;

namespace Buffalo
{
    public class MethodDetail
    {
        public MethodDetail()
        {
        }

        public string Name
        {
            get
            {
                StackTrace trace = new StackTrace();
                Console.WriteLine("frames: " + trace.FrameCount);
                return trace.GetFrame(2).GetMethod().Name;
            }
        }
    }
}
