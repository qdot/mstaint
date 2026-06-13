using MSTaint.Core;

namespace MSTaint.Windows;

internal sealed class WintabPenInputDecoder : IDisposable
{
    private IntPtr _context;
    private uint _maxPressure = PenSample.MaxPointerPressure;

    public bool TryOpen(IntPtr target, out string? error)
    {
        if (_context != IntPtr.Zero)
        {
            error = null;
            return true;
        }

        try
        {
            if (WintabNativeMethods.WTInfo(0, 0, IntPtr.Zero) == 0)
            {
                error = "Wintab32.dll is present but no tablet service/device is available.";
                return false;
            }

            var logContext = new WintabLogContext();
            var contextSize = WintabNativeMethods.WTInfo(
                WintabNativeMethods.WtiDefaultSystemContext,
                index: 0,
                ref logContext);
            if (contextSize == 0)
            {
                error = "default WinTab system context is unavailable.";
                return false;
            }

            ConfigureContext(ref logContext);
            _maxPressure = GetMaxPressure();

            _context = WintabNativeMethods.WTOpen(target, ref logContext, enable: true);
            if (_context == IntPtr.Zero)
            {
                error = "WTOpen failed for the pressure context.";
                return false;
            }

            WintabNativeMethods.WTEnable(_context, true);
            error = null;
            return true;
        }
        catch (DllNotFoundException)
        {
            error = "Wintab32.dll was not found.";
            return false;
        }
        catch (BadImageFormatException ex)
        {
            error = $"Wintab32.dll could not be loaded by this process: {ex.Message}";
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            error = $"required WinTab entry point missing: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            error = $"WinTab initialization failed: {ex.Message}";
            return false;
        }
    }

    public bool TryDecode(Message message, out PenSample sample)
    {
        sample = default!;

        if (_context == IntPtr.Zero ||
            message.Msg != WintabNativeMethods.WTPacketMessage ||
            message.LParam != _context)
        {
            return false;
        }

        var packetSerialNumber = unchecked((uint)message.WParam.ToInt64());
        if (packetSerialNumber == 0 ||
            !WintabNativeMethods.WTPacket(_context, packetSerialNumber, out var packet))
        {
            return false;
        }

        var pressure = NormalizePressure(packet.NormalPressure);
        var pointerFlags = PenPointerFlags.Update | PenPointerFlags.InRange;
        if (pressure > 0)
        {
            pointerFlags |= PenPointerFlags.InContact;
        }

        sample = PenSample.Create(
            pointerId: unchecked((uint)_context.GetHashCode()),
            DateTimeOffset.UtcNow,
            new PenPosition(packet.X, packet.Y),
            inRange: true,
            inContact: pressure > 0,
            pressure,
            penMask: PenMask.Pressure,
            pointerFlags: pointerFlags);
        return true;
    }

    public void Dispose()
    {
        if (_context == IntPtr.Zero)
        {
            return;
        }

        WintabNativeMethods.WTClose(_context);
        _context = IntPtr.Zero;
    }

    private static void ConfigureContext(ref WintabLogContext context)
    {
        context.Name = "MSTaint WinTab";
        context.Options |= WintabNativeMethods.ContextOptionSystem | WintabNativeMethods.ContextOptionMessages;
        context.PacketData = WintabNativeMethods.PressurePacketData;
        context.PacketMode = WintabNativeMethods.PressurePacketMode;
        context.MoveMask = WintabNativeMethods.PressurePacketData;
        context.ButtonUpMask = context.ButtonDownMask;

        var xAxis = GetAxis(WintabNativeMethods.DeviceX);
        var yAxis = GetAxis(WintabNativeMethods.DeviceY);
        if (xAxis.Max > xAxis.Min)
        {
            context.InputOriginX = xAxis.Min;
            context.InputExtentX = xAxis.Max - xAxis.Min;
        }

        if (yAxis.Max > yAxis.Min)
        {
            context.InputOriginY = yAxis.Min;
            context.InputExtentY = yAxis.Max - yAxis.Min;
        }
    }

    private static WintabAxis GetAxis(uint deviceIndex)
    {
        var axis = new WintabAxis();
        WintabNativeMethods.WTInfo(WintabNativeMethods.WtiDevices, deviceIndex, ref axis);
        return axis;
    }

    private static uint GetMaxPressure()
    {
        var axis = GetAxis(WintabNativeMethods.DeviceNormalPressure);
        return axis.Max > axis.Min ? (uint)(axis.Max - axis.Min) : PenSample.MaxPointerPressure;
    }

    private uint NormalizePressure(uint pressure)
    {
        if (_maxPressure == 0 || _maxPressure == PenSample.MaxPointerPressure)
        {
            return Math.Min(pressure, PenSample.MaxPointerPressure);
        }

        return (uint)Math.Round(
            Math.Clamp(pressure / (double)_maxPressure, 0.0, 1.0) * PenSample.MaxPointerPressure);
    }
}
