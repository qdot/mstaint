using System.Runtime.InteropServices;

namespace MSTaint.Windows;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct WintabAxis
{
    public int Min;
    public int Max;
    public uint Units;
    public uint Resolution;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct WintabLogContext
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
    public string Name;
    public uint Options;
    public uint Status;
    public uint Locks;
    public uint MessageBase;
    public uint Device;
    public uint PacketRate;
    public uint PacketData;
    public uint PacketMode;
    public uint MoveMask;
    public uint ButtonDownMask;
    public uint ButtonUpMask;
    public int InputOriginX;
    public int InputOriginY;
    public int InputOriginZ;
    public int InputExtentX;
    public int InputExtentY;
    public int InputExtentZ;
    public int OutputOriginX;
    public int OutputOriginY;
    public int OutputOriginZ;
    public int OutputExtentX;
    public int OutputExtentY;
    public int OutputExtentZ;
    public uint SensitivityX;
    public uint SensitivityY;
    public uint SensitivityZ;
    public int SystemMode;
    public int SystemOriginX;
    public int SystemOriginY;
    public int SystemExtentX;
    public int SystemExtentY;
    public uint SystemSensitivityX;
    public uint SystemSensitivityY;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WintabPressurePacket
{
    public uint Buttons;
    public int X;
    public int Y;
    public uint NormalPressure;
}
