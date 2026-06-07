using WindowsPenControl.Core;

namespace WindowsPenControl.Windows;

internal static class PenPointerDecoder
{
    private const int WM_POINTERUPDATE = 0x0245;
    private const int WM_POINTERDOWN = 0x0246;
    private const int WM_POINTERUP = 0x0247;
    private const int WM_POINTERENTER = 0x0249;
    private const int WM_POINTERLEAVE = 0x024A;

    public static bool TryDecode(Message message, out PenSample sample)
    {
        sample = default!;

        if (!IsPointerMessage(message.Msg))
        {
            return false;
        }

        var pointerId = GetPointerId(message.WParam);
        if (!NativeMethods.GetPointerPenInfo(pointerId, out var penInfo))
        {
            return false;
        }

        var pointerFlags = (PenPointerFlags)penInfo.PointerInfo.PointerFlags;
        sample = PenSample.Create(
            pointerId,
            DateTimeOffset.UtcNow,
            new PenPosition(
                penInfo.PointerInfo.PixelLocation.X,
                penInfo.PointerInfo.PixelLocation.Y),
            pointerFlags.HasFlag(PenPointerFlags.InRange),
            pointerFlags.HasFlag(PenPointerFlags.InContact),
            penInfo.Pressure,
            penInfo.TiltX,
            penInfo.TiltY,
            penInfo.Rotation,
            (PenFlags)penInfo.PenFlags,
            (PenMask)penInfo.PenMask,
            pointerFlags);

        return true;
    }

    private static bool IsPointerMessage(int message) =>
        message is WM_POINTERUPDATE or WM_POINTERDOWN or WM_POINTERUP or WM_POINTERENTER or WM_POINTERLEAVE;

    private static uint GetPointerId(IntPtr wParam) => (uint)(wParam.ToInt64() & 0xffff);
}

