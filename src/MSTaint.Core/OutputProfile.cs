namespace MSTaint.Core;

public sealed record OutputProfile
{
    public static OutputProfile Default { get; } = new();

    public bool IsArmed { get; init; }

    public uint PressureMin { get; init; } = 0;

    public uint PressureMax { get; init; } = PenSample.MaxPointerPressure;

    public double Deadzone { get; init; } = 0.03;

    public double Curve { get; init; } = 1.3;

    public TimeSpan SmoothingWindow { get; init; } = TimeSpan.FromMilliseconds(30);

    public TimeSpan StaleTimeout { get; init; } = TimeSpan.FromMilliseconds(150);

    public IReadOnlySet<uint> SelectedDeviceIds { get; init; } = new HashSet<uint>();

    public void Validate()
    {
        if (PressureMax <= PressureMin)
        {
            throw new InvalidOperationException("PressureMax must be greater than PressureMin.");
        }

        if (Deadzone is < 0 or >= 1)
        {
            throw new InvalidOperationException("Deadzone must be in the range [0, 1).");
        }

        if (Curve <= 0)
        {
            throw new InvalidOperationException("Curve must be greater than zero.");
        }

        if (SmoothingWindow < TimeSpan.Zero)
        {
            throw new InvalidOperationException("SmoothingWindow cannot be negative.");
        }

        if (StaleTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("StaleTimeout must be greater than zero.");
        }
    }
}

