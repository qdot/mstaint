namespace MSTaint;

internal sealed class SettingsForm : Form
{
    private readonly CheckBox _armedCheckBox;
    private readonly Label _stateValue;
    private readonly Label _intensityValue;
    private readonly ProgressBar _intensityBar;
    private readonly Label _captureValue;
    private readonly Label _intifaceValue;
    private readonly ListBox _devicesList;
    private readonly Button _connectButton;
    private readonly Button _scanButton;
    private bool _isUpdating;

    public SettingsForm()
    {
        var displayName = GetDisplayName();
        Text = displayName;
        ClientSize = new Size(600, 480);
        MinimumSize = new Size(620, 540);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(14),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new Label
        {
            Text = displayName,
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 10),
        };

        var statusGrid = CreateStatusGrid();
        _armedCheckBox = new CheckBox
        {
            Text = "Armed",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
        };
        _armedCheckBox.CheckedChanged += (_, _) =>
        {
            if (!_isUpdating)
            {
                ArmedChanged?.Invoke(this, _armedCheckBox.Checked);
            }
        };

        _stateValue = CreateValueLabel(singleLine: true);
        _intensityValue = CreateValueLabel(singleLine: true);
        _intensityBar = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 1000,
            Height = 22,
            Margin = new Padding(0, 4, 0, 0),
        };
        var intensityPanel = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
        };
        intensityPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        intensityPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        intensityPanel.Controls.Add(_intensityValue, 0, 0);
        intensityPanel.Controls.Add(_intensityBar, 0, 1);

        _captureValue = CreateValueLabel(singleLine: false);
        _intifaceValue = CreateValueLabel(singleLine: false);

        AddStatusRow(statusGrid, 0, "State", _stateValue, 34);
        AddStatusRow(statusGrid, 1, "Intensity", intensityPanel, 62);
        AddStatusRow(statusGrid, 2, "Capture", _captureValue, 54);
        AddStatusRow(statusGrid, 3, "Intiface", _intifaceValue, 54);

        var devicesPanel = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 10, 0, 10),
        };
        devicesPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        devicesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        devicesPanel.Controls.Add(new Label
        {
            Text = "Devices",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4),
        }, 0, 0);

        _devicesList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
        };
        devicesPanel.Controls.Add(_devicesList, 0, 1);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = Padding.Empty,
        };

        _connectButton = CreateButton("Connect Intiface");
        _connectButton.Click += (_, _) => ConnectRequested?.Invoke(this, EventArgs.Empty);
        buttonPanel.Controls.Add(_connectButton);

        _scanButton = CreateButton("Scan Devices");
        _scanButton.Click += (_, _) => ScanRequested?.Invoke(this, EventArgs.Empty);
        buttonPanel.Controls.Add(_scanButton);

        var penTestButton = CreateButton("Pen Test");
        penTestButton.Click += (_, _) => PenTestRequested?.Invoke(this, EventArgs.Empty);
        buttonPanel.Controls.Add(penTestButton);

        var stopButton = CreateButton("Emergency Stop");
        stopButton.Click += (_, _) => EmergencyStopRequested?.Invoke(this, EventArgs.Empty);
        buttonPanel.Controls.Add(stopButton);

        var exitButton = CreateButton("Exit");
        exitButton.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        buttonPanel.Controls.Add(exitButton);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_armedCheckBox, 0, 1);
        root.Controls.Add(statusGrid, 0, 2);
        root.Controls.Add(devicesPanel, 0, 3);
        root.Controls.Add(buttonPanel, 0, 4);
        Controls.Add(root);
    }

    public event EventHandler<bool>? ArmedChanged;

    public event EventHandler? ConnectRequested;

    public event EventHandler? ScanRequested;

    public event EventHandler? PenTestRequested;

    public event EventHandler? EmergencyStopRequested;

    public event EventHandler? ExitRequested;

    public void UpdateSnapshot(SettingsSnapshot snapshot)
    {
        if (IsDisposed)
        {
            return;
        }

        _isUpdating = true;
        var devicesUpdating = false;
        try
        {
            _armedCheckBox.Checked = snapshot.IsArmed;
            _stateValue.Text = snapshot.IsArmed ? "Armed" : "Disarmed";
            _intensityValue.Text = snapshot.Intensity.ToString("0.000");
            _intensityBar.Value = (int)Math.Round(Math.Clamp(snapshot.Intensity, 0.0, 1.0) * _intensityBar.Maximum);
            _captureValue.Text = snapshot.CaptureStatus;
            _intifaceValue.Text = snapshot.OutputStatus;
            _connectButton.Enabled = !snapshot.IsIntifaceConnected;
            _scanButton.Enabled = snapshot.IsIntifaceConnected;

            _devicesList.BeginUpdate();
            devicesUpdating = true;
            _devicesList.Items.Clear();
            if (snapshot.Devices.Count == 0)
            {
                _devicesList.Items.Add("none");
            }
            else
            {
                foreach (var device in snapshot.Devices)
                {
                    _devicesList.Items.Add(FormatDevice(device));
                }
            }
        }
        finally
        {
            if (devicesUpdating)
            {
                _devicesList.EndUpdate();
            }

            _isUpdating = false;
        }
    }

    private static TableLayoutPanel CreateStatusGrid()
    {
        var grid = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 4,
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = Padding.Empty,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return grid;
    }

    private static void AddStatusRow(
        TableLayoutPanel grid,
        int row,
        string caption,
        Control valueControl,
        int height)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        grid.Controls.Add(new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 8, 6),
        }, 0, row);
        valueControl.Margin = new Padding(0, 0, 0, 6);
        grid.Controls.Add(valueControl, 1, row);
    }

    private static Label CreateValueLabel(bool singleLine)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = singleLine,
            BorderStyle = BorderStyle.FixedSingle,
            MinimumSize = new Size(0, 28),
            Padding = new Padding(8, 3, 8, 3),
            TextAlign = ContentAlignment.MiddleLeft,
        };
    }

    private static Button CreateButton(string text)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            Height = 32,
            MinimumSize = new Size(96, 32),
            Margin = new Padding(0, 0, 8, 8),
            UseVisualStyleBackColor = true,
        };
    }

    private static string FormatDevice(HapticDevice device)
    {
        return device.HasVibrate ? device.Name : $"{device.Name} (no vibrate)";
    }

    private static string GetDisplayName()
    {
        var version = typeof(SettingsForm).Assembly.GetName().Version;
        return version is null
            ? "MSTaint"
            : $"MSTaint {version.ToString(3)}";
    }
}

internal sealed record SettingsSnapshot(
    bool IsArmed,
    bool IsIntifaceConnected,
    double Intensity,
    string CaptureStatus,
    string OutputStatus,
    IReadOnlyList<HapticDevice> Devices);
