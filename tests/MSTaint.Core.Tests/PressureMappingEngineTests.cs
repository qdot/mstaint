using MSTaint.Core;

namespace MSTaint.Core.Tests;

public sealed class PressureMappingEngineTests
{
    [Fact]
    public void MapsPressureThroughCalibration()
    {
        var mapper = new PressureMappingEngine();
        var profile = OutputProfile.Default with
        {
            IsArmed = true,
            PressureMin = 100,
            PressureMax = 900,
            Deadzone = 0,
            Curve = 1,
            SmoothingWindow = TimeSpan.Zero,
        };

        var intensity = mapper.Map(Sample(500), profile);

        Assert.Equal(0.5, intensity, precision: 3);
    }

    [Fact]
    public void AppliesDeadzone()
    {
        var mapper = new PressureMappingEngine();
        var profile = OutputProfile.Default with
        {
            IsArmed = true,
            Deadzone = 0.25,
            Curve = 1,
            SmoothingWindow = TimeSpan.Zero,
        };

        Assert.Equal(0, mapper.Map(Sample(128), profile));
    }

    [Fact]
    public void AppliesCurve()
    {
        var mapper = new PressureMappingEngine();
        var profile = OutputProfile.Default with
        {
            IsArmed = true,
            Deadzone = 0,
            Curve = 2,
            SmoothingWindow = TimeSpan.Zero,
        };

        var intensity = mapper.Map(Sample(512), profile);

        Assert.Equal(0.25, intensity, precision: 3);
    }

    [Fact]
    public void SmoothsTowardNewPressureValue()
    {
        var mapper = new PressureMappingEngine();
        var profile = OutputProfile.Default with
        {
            IsArmed = true,
            Deadzone = 0,
            Curve = 1,
            SmoothingWindow = TimeSpan.FromMilliseconds(100),
        };
        var start = DateTimeOffset.Parse("2026-06-07T12:00:00Z");

        mapper.Map(Sample(0, start), profile);
        var intensity = mapper.Map(Sample(1024, start.AddMilliseconds(25)), profile);

        Assert.Equal(0.25, intensity, precision: 3);
    }

    [Fact]
    public void ReturnsZeroWhenPressureIsMissing()
    {
        var mapper = new PressureMappingEngine();
        var profile = OutputProfile.Default with { IsArmed = true };

        var intensity = mapper.Map(Sample(900, penMask: PenMask.None), profile);

        Assert.Equal(0, intensity);
    }

    private static PenSample Sample(
        uint pressure,
        DateTimeOffset? timestamp = null,
        PenMask penMask = PenMask.Pressure) =>
        PenSample.Create(
            1,
            timestamp ?? DateTimeOffset.Parse("2026-06-07T12:00:00Z"),
            new PenPosition(0, 0),
            inRange: true,
            inContact: true,
            pressureRaw: pressure,
            penMask: penMask);
}

