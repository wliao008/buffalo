using Mono.Cecil.Cil;

namespace Buffalo
{
    internal struct TcfMarker
    {
        public Instruction TryInnerStart { get; set; }
        public Instruction TryInnerEnd { get; set; }
        public Instruction TryOuterStart { get; set; }
        public Instruction TryOuterEnd { get; set; }
        public Instruction CatchInnerStart { get; set; }
        public Instruction CatchInnerEnd { get; set; }
        public Instruction FinallyStart { get; set; }
        public Instruction FinallyEnd { get; set; }

        //deprecated
        public Instruction TryStart { get; set; }
        public Instruction TryEnd { get; set; }
        public Instruction HandlerStart { get; set; }
        public Instruction HandlerEnd { get; set; }
    }

    internal struct BeginEndMarker
    {
        public int BeginIndex { get; set; }
        public int EndIndex { get; set; }
    }
}
