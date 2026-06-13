using MSTaint.Core;

namespace MSTaint;

public sealed class TrayApplicationContext : ApplicationContext
{
    private static readonly Uri DefaultIntifaceUri = new("ws://127.0.0.1:12345");

    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _armItem;
    private readonly ToolStripMenuItem _devicesItem;
    private readonly PenInputService _penInput = new();
    private readonly ButtplugOutputService _output = new();
    private readonly PenHapticsController _controller = new();
    private readonly System.Windows.Forms.Timer _staleTimer = new();
    private readonly SynchronizationContext _uiContext;
    private SettingsForm? _settingsForm;
    private PenTelemetryForm? _telemetryForm;
    private bool _isExiting;
    private string _captureStatus = "Global capture not started";
    private string _outputStatus = "Intiface not connected";

    public TrayApplicationContext()
    {
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        _statusItem = new ToolStripMenuItem("Starting") { Enabled = false };
        _armItem = new ToolStripMenuItem("Armed", null, (_, _) => ToggleArmed()) { Checked = false };
        _devicesItem = new ToolStripMenuItem("Devices: none") { Enabled = false };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(_devicesItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Open Settings", null, (_, _) => OpenSettingsWindow()));
        menu.Items.Add(_armItem);
        menu.Items.Add(new ToolStripMenuItem("Connect Intiface", null, async (_, _) => await ConnectIntifaceAsync()));
        menu.Items.Add(new ToolStripMenuItem("Scan Devices", null, async (_, _) => await ScanDevicesAsync()));
        menu.Items.Add(new ToolStripMenuItem("Open Pen Test Window", null, (_, _) => OpenTelemetryWindow()));
        menu.Items.Add(new ToolStripMenuItem("Emergency Stop", null, async (_, _) => await EmergencyStopAsync("manual-stop")));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, async (_, _) => await ExitAsync()));

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = SystemIcons.Application,
            Text = "MSTaint",
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => OpenSettingsWindow();

        _penInput.SampleReceived += async (_, sample) => await HandlePenSampleAsync(sample);
        _penInput.CaptureError += (_, message) =>
        {
            _captureStatus = message;
            UpdateStatus();
        };

        _output.StatusChanged += (_, message) =>
        {
            PostToUi(() =>
            {
                _outputStatus = message;
                UpdateStatus();
            });
        };
        _output.DevicesChanged += (_, _) => PostToUi(UpdateDevices);

        _staleTimer.Interval = 50;
        _staleTimer.Tick += async (_, _) => await CheckStaleAsync();
        _staleTimer.Start();

        _penInput.Start();
        _penInput.TryStartGlobalCapture();
        _captureStatus = _penInput.CaptureStatus;

        UpdateStatus();
        UpdateDevices();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _settingsForm?.Close();
            _telemetryForm?.Close();
            _staleTimer.Dispose();
            _penInput.Dispose();
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ToggleArmed()
    {
        SetArmed(!_controller.Profile.IsArmed);
    }

    private void SetArmed(bool isArmed)
    {
        if (_controller.Profile.IsArmed == isArmed)
        {
            _armItem.Checked = isArmed;
            UpdateStatus();
            return;
        }

        _controller.Profile = _controller.Profile with { IsArmed = isArmed };
        _armItem.Checked = isArmed;
        UpdateStatus();

        if (!isArmed)
        {
            _ = EmergencyStopAsync("disarmed");
        }
    }

    private void OpenSettingsWindow()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            UpdateSettingsWindow();
            _settingsForm.Activate();
            return;
        }

        _settingsForm = new SettingsForm();
        _settingsForm.ArmedChanged += (_, isArmed) => SetArmed(isArmed);
        _settingsForm.ConnectRequested += async (_, _) => await ConnectIntifaceAsync();
        _settingsForm.ScanRequested += async (_, _) => await ScanDevicesAsync();
        _settingsForm.PenTestRequested += (_, _) => OpenTelemetryWindow();
        _settingsForm.EmergencyStopRequested += async (_, _) => await EmergencyStopAsync("manual-stop");
        _settingsForm.ExitRequested += async (_, _) => await ExitAsync();
        _settingsForm.FormClosed += (_, _) => _settingsForm = null;

        UpdateSettingsWindow();
        _settingsForm.Show();
        _settingsForm.Activate();
    }

    private async Task ConnectIntifaceAsync()
    {
        try
        {
            await _output.ConnectAsync(DefaultIntifaceUri).ConfigureAwait(true);
            await _output.StartScanAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _outputStatus = $"Intiface connection failed: {ex.Message}";
            UpdateStatus();
        }
    }

    private async Task ScanDevicesAsync()
    {
        try
        {
            await _output.StartScanAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _outputStatus = $"Scan failed: {ex.Message}";
            UpdateStatus();
        }
    }

    private void OpenTelemetryWindow()
    {
        if (_telemetryForm is { IsDisposed: false })
        {
            _telemetryForm.Activate();
            return;
        }

        _telemetryForm = new PenTelemetryForm(sample => _ = HandlePenSampleAsync(sample));
        _telemetryForm.Show();
    }

    private async Task HandlePenSampleAsync(PenSample sample)
    {
        try
        {
            await _controller.HandleSampleAsync(sample, _output).ConfigureAwait(true);
            UpdateStatus();
        }
        catch (Exception ex)
        {
            _outputStatus = $"Output failed: {ex.Message}";
            await EmergencyStopAsync("output-error").ConfigureAwait(true);
            UpdateStatus();
        }
    }

    private async Task CheckStaleAsync()
    {
        try
        {
            await _controller.CheckStaleAsync(DateTimeOffset.UtcNow, _output).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _outputStatus = $"Stale check failed: {ex.Message}";
            UpdateStatus();
        }
    }

    private async Task EmergencyStopAsync(string reason)
    {
        try
        {
            await _controller.StopAsync(_output, reason).ConfigureAwait(true);
        }
        finally
        {
            _armItem.Checked = _controller.Profile.IsArmed;
            UpdateStatus();
        }
    }

    private async Task ExitAsync()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _controller.Profile = _controller.Profile with { IsArmed = false };

        _notifyIcon.Visible = false;
        _staleTimer.Stop();
        _penInput.Dispose();
        _settingsForm?.Close();
        _telemetryForm?.Close();

        try
        {
            await _output.DisposeAsync().ConfigureAwait(true);
        }
        catch
        {
        }
        finally
        {
            ExitThread();
        }
    }

    private void UpdateDevices()
    {
        var devices = _output.Devices;
        _devicesItem.Text = devices.Count == 0
            ? "Devices: none"
            : $"Devices: {string.Join(", ", devices.Select(device => device.Name))}";
        UpdateSettingsWindow();
    }

    private void UpdateStatus()
    {
        var armed = _controller.Profile.IsArmed ? "armed" : "disarmed";
        var intensity = _controller.LastIntensity;
        _statusItem.Text = $"{armed}; intensity {intensity:0.000}; {_captureStatus}; {_outputStatus}";
        _notifyIcon.Text = _statusItem.Text.Length <= 63
            ? _statusItem.Text
            : _statusItem.Text[..63];
        UpdateSettingsWindow();
    }

    private void UpdateSettingsWindow()
    {
        if (_settingsForm is null || _settingsForm.IsDisposed)
        {
            return;
        }

        _settingsForm.UpdateSnapshot(new SettingsSnapshot(
            _controller.Profile.IsArmed,
            _output.IsConnected,
            _controller.LastIntensity,
            _captureStatus,
            _outputStatus,
            _output.Devices));
    }

    private void PostToUi(Action action)
    {
        if (_isExiting)
        {
            return;
        }

        if (SynchronizationContext.Current == _uiContext)
        {
            action();
            return;
        }

        _uiContext.Post(_ => action(), null);
    }
}

