using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsPenControl.Windows;

internal static partial class NativeMethods
{
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetPointerPenInfo(uint pointerId, out PointerPenInfo penInfo);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterPointerInputTarget(IntPtr hwnd, PointerInputType pointerType);

    public static string GetLastErrorMessage()
    {
        var error = Marshal.GetLastPInvokeError();
        return error == 0 ? "unknown error" : new Win32Exception(error).Message;
    }
}

