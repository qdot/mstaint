using WindowsPenControl.Core;
using WindowsPenControl.Windows;

namespace WindowsPenControl;

public sealed class PenInputService : IDisposable
{
    private PenMessageWindow? _window;

    public event EventHandler<PenSample>? SampleReceived;

    public event EventHandler<string>? CaptureError;

    public bool IsRunning => _window is not null;

    public void Start()
    {
        if (_window is not null)
        {
            return;
        }

        _window = new PenMessageWindow(sample => SampleReceived?.Invoke(this, sample));
        _window.Create();
    }

    public bool TryStartGlobalCapture()
    {
        Start();

        if (_window is null)
        {
            CaptureError?.Invoke(this, "Pen capture window was not created.");
            return false;
        }

        if (NativeMethods.RegisterPointerInputTarget(_window.Handle, PointerInputType.Pen))
        {
            return true;
        }

        var error = NativeMethods.GetLastErrorMessage();
        CaptureError?.Invoke(this, $"Global pen capture failed: {error}");
        return false;
    }

    public void Stop()
    {
        _window?.Destroy();
        _window = null;
    }

    public void Dispose()
    {
        Stop();
    }

    private sealed class PenMessageWindow(Action<PenSample> onSample) : NativeWindow
    {
        public void Create()
        {
            CreateHandle(new CreateParams
            {
                Caption = "Windows Pen Control Capture",
                ExStyle = NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW,
            });
        }

        public void Destroy()
        {
            DestroyHandle();
        }

        protected override void WndProc(ref Message m)
        {
            if (PenPointerDecoder.TryDecode(m, out var sample))
            {
                onSample(sample);
            }

            base.WndProc(ref m);
        }
    }
}

