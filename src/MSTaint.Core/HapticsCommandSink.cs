namespace MSTaint.Core;

public interface IHapticsCommandSink
{
    Task SetIntensityAsync(IReadOnlyCollection<uint> deviceIds, double intensity, CancellationToken cancellationToken = default);

    Task StopAllAsync(string reason, CancellationToken cancellationToken = default);
}

