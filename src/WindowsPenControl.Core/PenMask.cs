namespace WindowsPenControl.Core;

[Flags]
public enum PenMask : uint
{
    None = 0,
    Pressure = 0x00000001,
    Rotation = 0x00000002,
    TiltX = 0x00000004,
    TiltY = 0x00000008,
}

