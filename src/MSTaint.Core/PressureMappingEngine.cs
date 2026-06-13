namespace MSTaint.Core;

public sealed class PressureMappingEngine
{
    private double? _smoothedIntensity;
    private DateTimeOffset? _lastTimestamp;

    public double Map(PenSample sample, OutputProfile profile)
    {
        profile.Validate();

        if (!profile.IsArmed ||
            !sample.InContact ||
            !sample.PenMask.HasFlag(PenMask.Pressure))
        {
            Reset();
            return 0;
        }

        var calibrated = NormalizePressure(sample.PressureRaw, profile);
        var curved = ApplyDeadzoneAndCurve(calibrated, profile);
        var smoothed = ApplySmoothing(curved, sample.Timestamp, profile.SmoothingWindow);

        return Math.Clamp(smoothed, 0, 1);
    }

    public void Reset()
    {
        _smoothedIntensity = null;
        _lastTimestamp = null;
    }

    private static double NormalizePressure(uint pressureRaw, OutputProfile profile)
    {
        var clamped = Math.Clamp(pressureRaw, profile.PressureMin, profile.PressureMax);
        return (clamped - profile.PressureMin) / (double)(profile.PressureMax - profile.PressureMin);
    }

    private static double ApplyDeadzoneAndCurve(double value, OutputProfile profile)
    {
        if (value <= profile.Deadzone)
        {
            return 0;
        }

        var deadzoneAdjusted = (value - profile.Deadzone) / (1 - profile.Deadzone);
        return Math.Pow(deadzoneAdjusted, profile.Curve);
    }

    private double ApplySmoothing(double value, DateTimeOffset timestamp, TimeSpan smoothingWindow)
    {
        if (_smoothedIntensity is not { } previous ||
            _lastTimestamp is not { } lastTimestamp ||
            smoothingWindow == TimeSpan.Zero)
        {
            _smoothedIntensity = value;
            _lastTimestamp = timestamp;
            return value;
        }

        var elapsed = timestamp - lastTimestamp;
        var alpha = elapsed <= TimeSpan.Zero
            ? 1
            : Math.Clamp(elapsed.TotalMilliseconds / smoothingWindow.TotalMilliseconds, 0, 1);

        _smoothedIntensity = previous + ((value - previous) * alpha);
        _lastTimestamp = timestamp;

        return _smoothedIntensity.Value;
    }
}

