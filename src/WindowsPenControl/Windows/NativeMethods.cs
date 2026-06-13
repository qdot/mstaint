using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsPenControl.Windows;

internal static partial class NativeMethods
{
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WM_INPUT = 0x00FF;
    public const uint HIDP_STATUS_SUCCESS = 0x00110000;
    public const uint HID_USAGE_PAGE_DIGITIZER = 0x0D;
    public const uint RID_INPUT = 0x10000003;
    public const uint RIDI_PREPARSEDDATA = 0x20000005;
    public const uint RIDEV_PAGEONLY = 0x00000020;
    public const uint RIDEV_INPUTSINK = 0x00000100;
    public const uint RIDEV_DEVNOTIFY = 0x00002000;
    public const uint RIM_TYPEHID = 2;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetPointerPenInfo(uint pointerId, out PointerPenInfo penInfo);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterPointerInputTarget(IntPtr hwnd, PointerInputType pointerType);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterPointerInputTarget(IntPtr hwnd, PointerInputType pointerType);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterRawInputDevices(
        [In] RawInputDevice[] rawInputDevices,
        uint numberDevices,
        uint size);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetRawInputData(
        IntPtr rawInput,
        uint command,
        IntPtr data,
        ref uint size,
        uint headerSize);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetRawInputDeviceInfo(
        IntPtr device,
        uint command,
        IntPtr data,
        ref uint size);

    [DllImport("hid.dll")]
    public static extern uint HidP_GetCaps(IntPtr preparsedData, out HidPCaps caps);

    [DllImport("hid.dll")]
    public static extern uint HidP_GetValueCaps(
        HidPReportType reportType,
        [Out] HidPValueCaps[] valueCaps,
        ref ushort valueCapsLength,
        IntPtr preparsedData);

    [DllImport("hid.dll")]
    public static extern uint HidP_GetUsageValue(
        HidPReportType reportType,
        ushort usagePage,
        ushort linkCollection,
        ushort usage,
        out uint usageValue,
        IntPtr preparsedData,
        byte[] report,
        uint reportLength);

    [DllImport("hid.dll")]
    public static extern uint HidP_GetScaledUsageValue(
        HidPReportType reportType,
        ushort usagePage,
        ushort linkCollection,
        ushort usage,
        out int usageValue,
        IntPtr preparsedData,
        byte[] report,
        uint reportLength);

    [DllImport("hid.dll")]
    public static extern uint HidP_GetUsages(
        HidPReportType reportType,
        ushort usagePage,
        ushort linkCollection,
        [Out] ushort[] usageList,
        ref uint usageLength,
        IntPtr preparsedData,
        byte[] report,
        uint reportLength);

    public static string GetLastErrorMessage()
    {
        var error = Marshal.GetLastPInvokeError();
        return error == 0 ? "unknown error" : new Win32Exception(error).Message;
    }
}

