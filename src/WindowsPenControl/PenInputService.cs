using WindowsPenControl.Core;
using WindowsPenControl.Windows;

namespace WindowsPenControl;

public sealed class PenInputService : IDisposable
{
    private PenMessageWindow? _window;

    public event EventHandler<PenSample>? SampleReceived;

    public event EventHandler<string>? CaptureError;

    public bool IsRunning => _window is not null;

    public string CaptureStatus { get; private set; } = "Pen capture not started";

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

        var enabledSources = new List<string>();
        var errors = new List<string>();
        var statusNotes = new List<string>();

        if (IsPointerRedirectEnabled())
        {
            if (_window.TryRegisterPointerRedirect(out var pointerError))
            {
                enabledSources.Add("Windows pointer redirect");
            }
            else
            {
                errors.Add($"Windows pointer redirect: {pointerError}");
            }
        }
        else
        {
            statusNotes.Add("Windows pointer redirect disabled");
        }

        if (_window.TryRegisterRawInput(out var rawInputError))
        {
            enabledSources.Add("raw HID");
        }
        else
        {
            errors.Add($"raw HID: {rawInputError}");
        }

        if (_window.TryOpenWintab(out var wintabError))
        {
            enabledSources.Add("WinTab");
        }
        else
        {
            errors.Add($"WinTab: {wintabError}");
        }

        if (enabledSources.Count > 0)
        {
            var notes = statusNotes.Count == 0 ? "" : $"; {string.Join("; ", statusNotes)}";
            CaptureStatus = $"Global pen capture active ({string.Join(" + ", enabledSources)}{notes})";
            return true;
        }

        var failureReasons = new List<string>(errors);
        failureReasons.AddRange(statusNotes);
        CaptureStatus = $"Global pen capture unavailable ({string.Join("; ", failureReasons)})";
        CaptureError?.Invoke(this, CaptureStatus);
        return false;
    }

    private static bool IsPointerRedirectEnabled()
    {
        var value = Environment.GetEnvironmentVariable("WPC_ENABLE_POINTER_REDIRECT");
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
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

    private enum PenInputSource
    {
        WindowsPointer,
        RawHid,
        Wintab,
    }

    private sealed class PenMessageWindow(Action<PenSample> onSample) : NativeWindow
    {
        private readonly RawPenInputDecoder _rawInput = new();
        private readonly WintabPenInputDecoder _wintab = new();
        private readonly PenSampleRouter _router = new(onSample);
        private bool _isPointerRedirectRegistered;

        public void Create()
        {
            CreateHandle(new CreateParams
            {
                Caption = "Windows Pen Control Capture",
                ExStyle = NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW,
            });
        }

        public bool TryRegisterPointerRedirect(out string? error)
        {
            if (NativeMethods.RegisterPointerInputTarget(Handle, PointerInputType.Pen))
            {
                _isPointerRedirectRegistered = true;
                error = null;
                return true;
            }

            error = NativeMethods.GetLastErrorMessage();
            return false;
        }

        public bool TryRegisterRawInput(out string? error)
        {
            return _rawInput.TryRegister(Handle, out error);
        }

        public bool TryOpenWintab(out string? error)
        {
            return _wintab.TryOpen(Handle, out error);
        }

        public void Destroy()
        {
            if (_isPointerRedirectRegistered)
            {
                NativeMethods.UnregisterPointerInputTarget(Handle, PointerInputType.Pen);
                _isPointerRedirectRegistered = false;
            }

            _wintab.Dispose();
            _rawInput.Dispose();
            DestroyHandle();
        }

        protected override void WndProc(ref Message m)
        {
            if (PenPointerDecoder.TryDecode(m, out var sample))
            {
                _router.Route(PenInputSource.WindowsPointer, sample);
            }

            if (_rawInput.TryDecode(m, out var rawSamples))
            {
                foreach (var rawSample in rawSamples)
                {
                    _router.Route(PenInputSource.RawHid, rawSample);
                }
            }

            if (_wintab.TryDecode(m, out var wintabSample))
            {
                _router.Route(PenInputSource.Wintab, wintabSample);
            }

            base.WndProc(ref m);
        }
    }

    private sealed class PenSampleRouter(Action<PenSample> onSample)
    {
        private static readonly TimeSpan ActiveSourceGracePeriod = TimeSpan.FromMilliseconds(500);
        private PenInputSource? _activeContactSource;
        private DateTimeOffset _activeSourceExpiresAt;

        public void Route(PenInputSource source, PenSample sample)
        {
            if (sample.InContact || sample.PressureRaw > 0)
            {
                _activeContactSource = source;
                _activeSourceExpiresAt = sample.Timestamp + ActiveSourceGracePeriod;
                onSample(sample);
                return;
            }

            if (_activeContactSource == source)
            {
                _activeContactSource = null;
                onSample(sample);
                return;
            }

            if (_activeContactSource is null || sample.Timestamp >= _activeSourceExpiresAt)
            {
                _activeContactSource = null;
                onSample(sample);
            }
        }
    }
}

