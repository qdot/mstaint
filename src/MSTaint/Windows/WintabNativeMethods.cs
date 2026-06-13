using System.Runtime.InteropServices;

namespace MSTaint.Windows;

internal static class WintabNativeMethods
{
    public const int WTPacketMessage = 0x7FF0;
    public const uint WtiDefaultSystemContext = 4;
    public const uint WtiDevices = 100;
    public const uint DeviceX = 12;
    public const uint DeviceY = 13;
    public const uint DeviceNormalPressure = 15;
    public const uint ContextOptionSystem = 0x0001;
    public const uint ContextOptionMessages = 0x0004;
    public const uint PacketButtons = 0x0040;
    public const uint PacketX = 0x0080;
    public const uint PacketY = 0x0100;
    public const uint PacketNormalPressure = 0x0400;
    public const uint PressurePacketData = PacketButtons | PacketX | PacketY | PacketNormalPressure;
    public const uint PressurePacketMode = PacketButtons;

    [DllImport("Wintab32.dll", EntryPoint = "WTInfoA")]
    public static extern uint WTInfo(uint category, uint index, IntPtr output);

    [DllImport("Wintab32.dll", EntryPoint = "WTInfoA")]
    public static extern uint WTInfo(uint category, uint index, ref WintabLogContext output);

    [DllImport("Wintab32.dll", EntryPoint = "WTInfoA")]
    public static extern uint WTInfo(uint category, uint index, ref WintabAxis output);

    [DllImport("Wintab32.dll", EntryPoint = "WTOpenA")]
    public static extern IntPtr WTOpen(IntPtr hwnd, ref WintabLogContext logContext, [MarshalAs(UnmanagedType.Bool)] bool enable);

    [DllImport("Wintab32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool WTClose(IntPtr context);

    [DllImport("Wintab32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool WTEnable(IntPtr context, [MarshalAs(UnmanagedType.Bool)] bool enable);

    [DllImport("Wintab32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool WTOverlap(IntPtr context, [MarshalAs(UnmanagedType.Bool)] bool toTop);

    [DllImport("Wintab32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool WTPacket(IntPtr context, uint packetSerialNumber, out WintabPressurePacket packet);
}
