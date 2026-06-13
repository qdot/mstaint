using MSTaint.Core;
using MSTaint.Windows;

namespace MSTaint;

public sealed class PenTelemetryForm : Form
{
    public PenTelemetryForm(Action<PenSample> onSample)
    {
        Text = "Pen Input Test";
        Width = 520;
        Height = 260;
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(new PenTelemetrySurface(onSample));
    }

    private sealed class PenTelemetrySurface : Label
    {
        private readonly Action<PenSample> _onSample;

        public PenTelemetrySurface(Action<PenSample> onSample)
        {
            _onSample = onSample;
            Dock = DockStyle.Fill;
            Font = new Font(FontFamily.GenericSansSerif, 11);
            Padding = new Padding(16);
            Text = "Draw in this window to validate local pen pressure, tilt, rotation, and buttons.";
            TextAlign = ContentAlignment.MiddleCenter;
        }

        protected override void WndProc(ref Message m)
        {
            if (PenPointerDecoder.TryDecode(m, out var sample))
            {
                _onSample(sample);
                Text = string.Join(
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
}

