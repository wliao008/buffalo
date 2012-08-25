using Mono.Cecil.Cil;

namespace Buffalo
{
    public struct TcfMarker
    {
        public Instruction TryStart { get; set; }
        public Instruction TryEnd { get; set; }
        public Instruction CatchStart { get; set; }
        public Instruction CatchEnd { get; set; }
        public Instruction FinallyStart { get; set; }
        public Instruction FinallyEnd { get; set; }
    }
}
