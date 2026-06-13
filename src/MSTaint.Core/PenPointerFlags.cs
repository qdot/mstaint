namespace MSTaint.Core;

[Flags]
public enum PenPointerFlags : uint
{
    None = 0,
    New = 0x00000001,
    InRange = 0x00000002,
    InContact = 0x00000004,
    FirstButton = 0x00000010,
    SecondButton = 0x00000020,
    ThirdButton = 0x00000040,
    FourthButton = 0x00000080,
    FifthButton = 0x00000100,
    Primary = 0x00002000,
    Confidence = 0x00004000,
    Canceled = 0x00008000,
    Down = 0x00010000,
    Update = 0x00020000,
    Up = 0x00040000,
    Wheel = 0x00080000,
    HWheel = 0x00100000,
    CaptureChanged = 0x00200000,
    HasTransform = 0x00400000,
}

