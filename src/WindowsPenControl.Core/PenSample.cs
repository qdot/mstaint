namespace WindowsPenControl.Core;

public sealed record PenSample(
    uint PointerId,
    DateTimeOffset Timestamp,
    PenPosition Position,
    bool InRange,
    bool InContact,
    uint PressureRaw,
    double PressureNormalized,
    int TiltX,
    int TiltY,
    uint Rotation,
    PenFlags PenFlags,
    PenMask PenMask,
    PenPointerFlags PointerFlags)
{
    public const uint MaxPointerPressure = 1024;

    public static PenSample Create(
        uint pointerId,
        DateTimeOffset timestamp,
        PenPosition position,
        bool inRange,
        bool inContact,
        uint pressureRaw,
        int tiltX = 0,
        int tiltY = 0,
        uint rotation = 0,
        PenFlags penFlags = PenFlags.None,
        PenMask penMask = PenMask.Pressure,
        PenPointerFlags pointerFlags = PenPointerFlags.None)
    {
        var clampedPressure = Math.Clamp(pressureRaw, 0, MaxPointerPressure);

        return new PenSample(
            pointerId,
            timestamp,
            position,
            inRange,
            inContact,
            clampedPressure,
            clampedPressure / (double)MaxPointerPressure,
            tiltX,
            tiltY,
            rotation,
            penFlags,
            penMask,
            pointerFlags);
    }
}

