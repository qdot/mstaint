namespace WindowsPenControl.Core;

public sealed class PenHapticsController
{
    private readonly PressureMappingEngine _mappingEngine = new();
    private bool _hasActiveOutput;

    public OutputProfile Profile { get; set; } = OutputProfile.Default;

    public DateTimeOffset? LastSampleAt { get; private set; }

    public double LastIntensity { get; private set; }

    public async Task HandleSampleAsync(
        PenSample sample,
        IHapticsCommandSink output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);

        Profile.Validate();
        LastSampleAt = sample.Timestamp;

        if (!Profile.IsArmed)
        {
            await StopIfNeededAsync(output, "disarmed", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!sample.InContact)
        {
            await StopIfNeededAsync(output, "pen-up", cancellationToken).ConfigureAwait(false);
            return;
        }

        var intensity = _mappingEngine.Map(sample, Profile);
        LastIntensity = intensity;

        if (intensity <= 0)
        {
            await StopIfNeededAsync(output, "zero-pressure", cancellationToken).ConfigureAwait(false);
            return;
        }

        await output.SetIntensityAsync(Profile.SelectedDeviceIds.ToArray(), intensity, cancellationToken)
            .ConfigureAwait(false);
        _hasActiveOutput = true;
    }

    public async Task CheckStaleAsync(
        DateTimeOffset now,
        IHapticsCommandSink output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (!_hasActiveOutput || LastSampleAt is not { } lastSampleAt)
        {
            return;
        }

        if (now - lastSampleAt >= Profile.StaleTimeout)
        {
            await StopIfNeededAsync(output, "stale-pen-input", cancellationToken).ConfigureAwait(false);
        }
    }

    public Task StopAsync(
        IHapticsCommandSink output,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);

        _mappingEngine.Reset();
        LastIntensity = 0;
        _hasActiveOutput = false;

        return output.StopAllAsync(reason, cancellationToken);
    }

    private async Task StopIfNeededAsync(
        IHapticsCommandSink output,
        string reason,
        CancellationToken cancellationToken)
    {
        _mappingEngine.Reset();
        LastIntensity = 0;

        if (!_hasActiveOutput)
        {
            return;
        }

        await output.StopAllAsync(reason, cancellationToken).ConfigureAwait(false);
        _hasActiveOutput = false;
    }
}
