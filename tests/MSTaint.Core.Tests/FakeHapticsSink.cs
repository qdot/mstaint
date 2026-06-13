using MSTaint.Core;

namespace MSTaint.Core.Tests;

internal sealed class FakeHapticsSink : IHapticsCommandSink
{
    public List<(IReadOnlyCollection<uint> DeviceIds, double Intensity)> Intensities { get; } = [];

    public List<string> StopReasons { get; } = [];

    public Task SetIntensityAsync(
        IReadOnlyCollection<uint> deviceIds,
        double intensity,
        CancellationToken cancellationToken = default)
    {
        Intensities.Add((deviceIds, intensity));
        return Task.CompletedTask;
    }

    public Task StopAllAsync(string reason, CancellationToken cancellationToken = default)
    {
        StopReasons.Add(reason);
        return Task.CompletedTask;
    }
}

