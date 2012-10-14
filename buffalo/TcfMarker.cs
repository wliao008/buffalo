﻿using Mono.Cecil.Cil;

namespace BuffaloAOP
{
    public struct TcfMarker
    {
        public Instruction TryStart { get; set; }
        public Instruction TryEnd { get; set; }
        public Instruction HandlerStart { get; set; }
        public Instruction HandlerEnd { get; set; }
        public Instruction FinallyStart { get; set; }
        public Instruction FinallyEnd { get; set; }
    }
}
