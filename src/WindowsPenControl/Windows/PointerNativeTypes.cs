using System.Runtime.InteropServices;

namespace WindowsPenControl.Windows;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct Point
{
    public readonly int X;
    public readonly int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct PointerInfo
{
    public readonly PointerInputType PointerType;
    public readonly uint PointerId;
    public readonly uint FrameId;
    public readonly uint PointerFlags;
    public readonly IntPtr SourceDevice;
    public readonly IntPtr HwndTarget;
    public readonly Point PixelLocation;
    public readonly Point HimetricLocation;
    public readonly Point PixelLocationRaw;
    public readonly Point HimetricLocationRaw;
    public readonly uint Time;
    public readonly uint HistoryCount;
    public readonly int InputData;
    public readonly uint KeyStates;
    public readonly ulong PerformanceCount;
    public readonly PointerButtonChangeType ButtonChangeType;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct PointerPenInfo
{
    public readonly PointerInfo PointerInfo;
    public readonly uint PenFlags;
    public readonly uint PenMask;
    public readonly uint Pressure;
    public readonly uint Rotation;
    public readonly int TiltX;
    public readonly int TiltY;
}

internal enum PointerButtonChangeType
{
    None,
    FirstButtonDown,
    FirstButtonUp,
    SecondButtonDown,
    SecondButtonUp,
    ThirdButtonDown,
    ThirdButtonUp,
    FourthButtonDown,
    FourthButtonUp,
    FifthButtonDown,
    FifthButtonUp,
}

