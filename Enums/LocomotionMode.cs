using System;

namespace Enums
{
    [Flags]
    public enum LocomotionMode : byte
    {
        None = 0b000,
        Sprint = 0b001,
        Walk = 0b010,
        Run = 0b100,
        FastWalk = 0b011,
        FastRun = 0b101,
        WalkAndRun = 0b110
    }
}