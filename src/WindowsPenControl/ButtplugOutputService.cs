using Buttplug.Client;
using Buttplug.Core.Messages;
using WindowsPenControl.Core;

namespace WindowsPenControl;

public sealed class ButtplugOutputService : IHapticsCommandSink, IAsyncDisposable
{
    private static readonly TimeSpan DisposeCommandTimeout = TimeSpan.FromSeconds(2);
    private readonly object _syncRoot = new();
    private ButtplugClient? _client;

    public event EventHandler? DevicesChanged;

    public event EventHandler<string>? StatusChanged;

    public bool IsConnected => _client?.Connected ?? false;

    public IReadOnlyList<HapticDevice> Devices
    {
        get
        {
            lock (_syncRoot)
            {
                return (_client?.Devices ?? [])
                    .Select(device => new HapticDevice(
                        device.Index,
                        device.DisplayName,
                        device.HasOutput(OutputType.Vibrate)))
                    .ToArray();
            }
        }
    }

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            return;
        }

        var client = new ButtplugClient("Windows Pen Control");
        client.DeviceAdded += (_, _) => DevicesChanged?.Invoke(this, EventArgs.Empty);
        client.DeviceRemoved += (_, _) => DevicesChanged?.Invoke(this, EventArgs.Empty);
        client.ScanningFinished += (_, _) =>
        {
            StatusChanged?.Invoke(this, "Device scan finished");
            DevicesChanged?.Invoke(this, EventArgs.Empty);
        };
        client.ServerDisconnect += (_, _) =>
        {
            StatusChanged?.Invoke(this, "Intiface disconnected");
            DevicesChanged?.Invoke(this, EventArgs.Empty);
        };
        client.ErrorReceived += (_, args) => StatusChanged?.Invoke(this, args.Exception.Message);

        await client.ConnectAsync(new ButtplugWebsocketConnector(uri), cancellationToken)
            .ConfigureAwait(false);

        lock (_syncRoot)
        {
            _client = client;
        }

        StatusChanged?.Invoke(this, $"Connected to {uri}");
        DevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task StartScanAsync(CancellationToken cancellationToken = default)
    {
        var client = _client ?? throw new InvalidOperationException("Connect to Intiface before scanning.");
        DevicesChanged?.Invoke(this, EventArgs.Empty);
        await client.StartScanningAsync(cancellationToken).ConfigureAwait(false);
        StatusChanged?.Invoke(this, "Scanning for devices");
        DevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SetIntensityAsync(
        IReadOnlyCollection<uint> deviceIds,
        double intensity,
        CancellationToken cancellationToken = default)
    {
        var client = _client;
        if (client is null || !client.Connected)
        {
            return;
        }

        var selectedDevices = client.Devices
            .Where(device => device.HasOutput(OutputType.Vibrate))
            .Where(device => deviceIds.Count == 0 || deviceIds.Contains(device.Index))
            .ToArray();

        var clamped = Math.Clamp(intensity, 0, 1);
        var command = new DeviceOutputCommand(OutputType.Vibrate, PercentOrSteps.FromPercent(clamped), null);

        foreach (var device in selectedDevices)
        {
            await device.RunOutputAsync(command, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task StopAllAsync(string reason, CancellationToken cancellationToken = default)
    {
        var client = _client;
        if (client is null || !client.Connected)
        {
            return;
        }

        await client.StopAllDevicesAsync(cancellationToken).ConfigureAwait(false);
        StatusChanged?.Invoke(this, $"Stopped: {reason}");
    }

    public async ValueTask DisposeAsync()
    {
        var client = _client;
        _client = null;

        if (client is null)
        {
            return;
        }

        if (client.Connected)
        {
            await TryWithTimeoutAsync(
                cancellationToken => client.StopScanningAsync(cancellationToken),
                DisposeCommandTimeout).ConfigureAwait(false);
            await TryWithTimeoutAsync(
                cancellationToken => client.StopAllDevicesAsync(cancellationToken),
                DisposeCommandTimeout).ConfigureAwait(false);
            await TryWithTimeoutAsync(
                () => client.DisconnectAsync(),
                DisposeCommandTimeout).ConfigureAwait(false);
        }

        await TryWithTimeoutAsync(
            () => client.DisposeAsync().AsTask(),
            DisposeCommandTimeout).ConfigureAwait(false);
    }

    private static async Task TryWithTimeoutAsync(
        Func<CancellationToken, Task> operation,
        TimeSpan timeout)
    {
        using var timeoutSource = new CancellationTokenSource(timeout);

        try
        {
            await operation(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (TimeoutException)
        {
        }
    }

    private static async Task TryWithTimeoutAsync(Func<Task> operation, TimeSpan timeout)
    {
        var operationTask = operation();
        var timeoutTask = Task.Delay(timeout);

        if (await Task.WhenAny(operationTask, timeoutTask).ConfigureAwait(false) == operationTask)
        {
            await operationTask.ConfigureAwait(false);
        }
    }
}

public sealed record HapticDevice(uint Id, string Name, bool HasVibrate);

