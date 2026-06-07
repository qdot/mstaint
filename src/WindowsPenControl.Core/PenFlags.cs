namespace WindowsPenControl.Core;

[Flags]
public enum PenFlags : uint
{
    None = 0,
    Barrel = 0x00000001,
    Inverted = 0x00000002,
    Eraser = 0x00000004,
}

