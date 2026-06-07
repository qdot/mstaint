using WindowsPenControl.Core;
using WindowsPenControl.Windows;

namespace WindowsPenControl;

public sealed class PenTelemetryForm : Form
{
    private readonly Action<PenSample> _onSample;
    private readonly Label _statusLabel;

    public PenTelemetryForm(Action<PenSample> onSample)
    {
        _onSample = onSample;
        Text = "Pen Input Test";
        Width = 520;
        Height = 260;
        StartPosition = FormStartPosition.CenterScreen;

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font(FontFamily.GenericSansSerif, 11),
            Padding = new Padding(16),
            Text = "Draw in this window to validate local pen pressure, tilt, rotation, and buttons.",
            TextAlign = ContentAlignment.MiddleCenter,
        };

        Controls.Add(_statusLabel);
    }

    protected override void WndProc(ref Message m)
    {
        if (PenPointerDecoder.TryDecode(m, out var sample))
        {
            _onSample(sample);
            _statusLabel.Text = string.Join(
                Environment.NewLine,
                $"Pointer: {sample.PointerId}",
                $"Contact: {sample.InContact}",
                $"Pressure: {sample.PressureRaw} ({sample.PressureNormalized:0.000})",
                $"Tilt: {sample.TiltX}, {sample.TiltY}",
                $"Rotation: {sample.Rotation}",
                $"Pen flags: {sample.PenFlags}",
                $"Pointer flags: {sample.PointerFlags}");
        }

        base.WndProc(ref m);
    }
}

