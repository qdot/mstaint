using WindowsPenControl.Core;

namespace WindowsPenControl.Core.Tests;

public sealed class PenHapticsControllerTests
{
    [Fact]
    public async Task DisarmedSampleDoesNotSendOutput()
    {
        var output = new FakeHapticsSink();
        var controller = new PenHapticsController
        {
            Profile = OutputProfile.Default with { IsArmed = false },
        };

        await controller.HandleSampleAsync(ContactSample(pressure: 800), output);

        Assert.Empty(output.Intensities);
        Assert.Empty(output.StopReasons);
        Assert.Equal(0, controller.LastIntensity);
    }

    [Fact]
    public async Task PenUpStopsActiveOutput()
    {
        var output = new FakeHapticsSink();
        var controller = ArmedController();

        await controller.HandleSampleAsync(ContactSample(pressure: 800), output);
        await controller.HandleSampleAsync(ContactSample(pressure: 0, inContact: false), output);

        Assert.Single(output.Intensities);
        Assert.Equal("pen-up", Assert.Single(output.StopReasons));
        Assert.Equal(0, controller.LastIntensity);
    }

    [Fact]
    public async Task StaleInputStopsActiveOutput()
    {
        var output = new FakeHapticsSink();
        var controller = ArmedController();
        var timestamp = DateTimeOffset.Parse("2026-06-07T12:00:00Z");

        await controller.HandleSampleAsync(ContactSample(pressure: 900, timestamp: timestamp), output);
        await controller.CheckStaleAsync(timestamp.AddMilliseconds(151), output);

        Assert.Single(output.Intensities);
        Assert.Equal("stale-pen-input", Assert.Single(output.StopReasons));
    }

    [Fact]
    public async Task SelectedDeviceIdsArePassedThrough()
    {
        var output = new FakeHapticsSink();
        var controller = ArmedController();
        controller.Profile = controller.Profile with
        {
            SelectedDeviceIds = new HashSet<uint> { 3, 7 },
        };

        await controller.HandleSampleAsync(ContactSample(pressure: 900), output);

        var command = Assert.Single(output.Intensities);
        Assert.Equal([3u, 7u], command.DeviceIds.Order().ToArray());
    }

    private static PenHapticsController ArmedController() => new()
    {
        Profile = OutputProfile.Default with
        {
            IsArmed = true,
            SmoothingWindow = TimeSpan.Zero,
        },
    };

    private static PenSample ContactSample(
        uint pressure,
        bool inContact = true,
        DateTimeOffset? timestamp = null) =>
        PenSample.Create(
            1,
            timestamp ?? DateTimeOffset.Parse("2026-06-07T12:00:00Z"),
            new PenPosition(10, 20),
            inRange: true,
            inContact: inContact,
            pressureRaw: pressure);
}
