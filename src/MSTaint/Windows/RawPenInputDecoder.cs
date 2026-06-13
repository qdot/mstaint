using System.Runtime.InteropServices;
using MSTaint.Core;

namespace MSTaint.Windows;

internal sealed class RawPenInputDecoder : IDisposable
{
    private const ushort UsagePageGenericDesktop = 0x01;
    private const ushort UsagePageDigitizer = 0x0D;
    private const ushort UsageX = 0x30;
    private const ushort UsageY = 0x31;
    private const ushort UsageTipPressure = 0x30;
    private const ushort UsageInRange = 0x32;
    private const ushort UsageInvert = 0x3C;
    private const ushort UsageXTilt = 0x3D;
    private const ushort UsageYTilt = 0x3E;
    private const ushort UsageTipSwitch = 0x42;
    private const ushort UsageBarrelSwitch = 0x44;
    private const ushort UsageEraser = 0x45;
    private static readonly uint RawInputHeaderSize = (uint)Marshal.SizeOf<RawInputHeader>();

    private readonly Dictionary<IntPtr, RawHidDevice> _devices = [];
    private readonly HashSet<IntPtr> _unsupportedDevices = [];

    public bool TryRegister(IntPtr target, out string? error)
    {
        var flags = NativeMethods.RIDEV_PAGEONLY
            | NativeMethods.RIDEV_INPUTSINK
            | NativeMethods.RIDEV_DEVNOTIFY;
        var devices = new[]
        {
            new RawInputDevice(
                (ushort)NativeMethods.HID_USAGE_PAGE_DIGITIZER,
                usage: 0,
                flags,
                target),
        };

        if (NativeMethods.RegisterRawInputDevices(
            devices,
            (uint)devices.Length,
            (uint)Marshal.SizeOf<RawInputDevice>()))
        {
            error = null;
            return true;
        }

        error = NativeMethods.GetLastErrorMessage();
        return false;
    }

    public bool TryDecode(Message message, out PenSample[] samples)
    {
        samples = [];

        if (message.Msg != NativeMethods.WM_INPUT)
        {
            return false;
        }

        var buffer = ReadRawInput(message.LParam, out var size);
        if (buffer == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return TryDecodeBuffer(buffer, size, out samples);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public void Dispose()
    {
        foreach (var device in _devices.Values)
        {
            device.Dispose();
        }

        _devices.Clear();
        _unsupportedDevices.Clear();
    }

    private bool TryDecodeBuffer(IntPtr buffer, uint size, out PenSample[] samples)
    {
        samples = [];

        if (size < RawInputHeaderSize + (2 * sizeof(uint)))
        {
            return false;
        }

        var header = Marshal.PtrToStructure<RawInputHeader>(buffer);
        if (header.Type != NativeMethods.RIM_TYPEHID)
        {
            return false;
        }

        var hidHeaderOffset = (int)RawInputHeaderSize;
        var sizeHid = unchecked((uint)Marshal.ReadInt32(buffer, hidHeaderOffset));
        var count = unchecked((uint)Marshal.ReadInt32(buffer, hidHeaderOffset + sizeof(uint)));
        if (sizeHid == 0 || count == 0 || sizeHid > int.MaxValue || count > int.MaxValue)
        {
            return false;
        }

        var rawDataOffset = hidHeaderOffset + (2 * sizeof(uint));
        var availableBytes = size - (uint)rawDataOffset;
        var totalReportBytes = (ulong)sizeHid * count;
        if (totalReportBytes > availableBytes)
        {
            return false;
        }

        var device = GetDevice(header.Device);
        if (device is null)
        {
            return false;
        }

        var decodedSamples = new List<PenSample>((int)count);
        var reportSource = IntPtr.Add(buffer, rawDataOffset);
        for (var index = 0; index < count; index++)
        {
            var report = CopyReport(
                IntPtr.Add(reportSource, checked((int)(index * sizeHid))),
                (int)sizeHid,
                device.InputReportByteLength);
            if (device.TryDecodeReport(header.Device, report, out var sample))
            {
                decodedSamples.Add(sample);
            }
        }

        samples = decodedSamples.ToArray();
        return samples.Length > 0;
    }

    private RawHidDevice? GetDevice(IntPtr deviceHandle)
    {
        if (_devices.TryGetValue(deviceHandle, out var device))
        {
            return device;
        }

        if (_unsupportedDevices.Contains(deviceHandle))
        {
            return null;
        }

        device = RawHidDevice.TryCreate(deviceHandle);
        if (device is { SupportsPressure: true })
        {
            _devices.Add(deviceHandle, device);
            return device;
        }

        device?.Dispose();
        _unsupportedDevices.Add(deviceHandle);
        return null;
    }

    private static IntPtr ReadRawInput(IntPtr rawInput, out uint size)
    {
        size = 0;
        var result = NativeMethods.GetRawInputData(
            rawInput,
            NativeMethods.RID_INPUT,
            IntPtr.Zero,
            ref size,
            RawInputHeaderSize);
        if (result == uint.MaxValue || size == 0)
        {
            return IntPtr.Zero;
        }

        var buffer = Marshal.AllocHGlobal(checked((int)size));
        result = NativeMethods.GetRawInputData(
            rawInput,
            NativeMethods.RID_INPUT,
            buffer,
            ref size,
            RawInputHeaderSize);
        if (result == uint.MaxValue)
        {
            Marshal.FreeHGlobal(buffer);
            return IntPtr.Zero;
        }

        return buffer;
    }

    private static byte[] CopyReport(IntPtr source, int sourceLength, ushort expectedLength)
    {
        var reportLength = Math.Max(sourceLength, expectedLength);
        var report = new byte[reportLength];
        Marshal.Copy(source, report, 0, sourceLength);
        return report;
    }

    private sealed class RawHidDevice : IDisposable
    {
        private readonly IntPtr _preparsedData;
        private readonly HidPValueCaps[] _valueCaps;

        private RawHidDevice(IntPtr preparsedData, HidPCaps caps, HidPValueCaps[] valueCaps)
        {
            _preparsedData = preparsedData;
            Caps = caps;
            _valueCaps = valueCaps;
            SupportsPressure = _valueCaps.Any(cap =>
                cap.UsagePage == UsagePageDigitizer && cap.ContainsUsage(UsageTipPressure));
        }

        public HidPCaps Caps { get; }

        public ushort InputReportByteLength => Caps.InputReportByteLength;

        public bool SupportsPressure { get; }

        public static RawHidDevice? TryCreate(IntPtr deviceHandle)
        {
            var preparsedDataSize = 0u;
            var result = NativeMethods.GetRawInputDeviceInfo(
                deviceHandle,
                NativeMethods.RIDI_PREPARSEDDATA,
                IntPtr.Zero,
                ref preparsedDataSize);
            if (result == uint.MaxValue || preparsedDataSize == 0)
            {
                return null;
            }

            var preparsedData = Marshal.AllocHGlobal(checked((int)preparsedDataSize));
            result = NativeMethods.GetRawInputDeviceInfo(
                deviceHandle,
                NativeMethods.RIDI_PREPARSEDDATA,
                preparsedData,
                ref preparsedDataSize);
            if (result == uint.MaxValue)
            {
                Marshal.FreeHGlobal(preparsedData);
                return null;
            }

            if (NativeMethods.HidP_GetCaps(preparsedData, out var caps) != NativeMethods.HIDP_STATUS_SUCCESS)
            {
                Marshal.FreeHGlobal(preparsedData);
                return null;
            }

            var valueCaps = GetValueCaps(preparsedData, caps);
            return new RawHidDevice(preparsedData, caps, valueCaps);
        }

        public bool TryDecodeReport(IntPtr deviceHandle, byte[] report, out PenSample sample)
        {
            sample = default!;

            if (!TryGetUsageValue(
                UsagePageDigitizer,
                UsageTipPressure,
                report,
                out var pressureValue,
                out var pressureCap))
            {
                return false;
            }

            var pressure = NormalizePressure(pressureValue, pressureCap);
            var position = new PenPosition(
                TryGetUsageValue(UsagePageGenericDesktop, UsageX, report, out var xValue, out var xCap)
                    ? ToInt32(xValue, xCap)
                    : 0,
                TryGetUsageValue(UsagePageGenericDesktop, UsageY, report, out var yValue, out var yCap)
                    ? ToInt32(yValue, yCap)
                    : 0);

            var xTilt = TryGetScaledUsageValue(UsagePageDigitizer, UsageXTilt, report, out var decodedXTilt)
                ? decodedXTilt
                : 0;
            var yTilt = TryGetScaledUsageValue(UsagePageDigitizer, UsageYTilt, report, out var decodedYTilt)
                ? decodedYTilt
                : 0;
            var activeUsages = GetActiveDigitizerUsages(report);
            var inContact = ContainsUsage(activeUsages, UsageTipSwitch) || pressure > 0;
            var inRange = ContainsUsage(activeUsages, UsageInRange) || inContact || SupportsPressure;
            var penFlags = PenFlags.None;
            if (ContainsUsage(activeUsages, UsageBarrelSwitch))
            {
                penFlags |= PenFlags.Barrel;
            }

            if (ContainsUsage(activeUsages, UsageInvert))
            {
                penFlags |= PenFlags.Inverted;
            }

            if (ContainsUsage(activeUsages, UsageEraser))
            {
                penFlags |= PenFlags.Eraser;
            }

            var penMask = PenMask.Pressure;
            if (xTilt != 0)
            {
                penMask |= PenMask.TiltX;
            }

            if (yTilt != 0)
            {
                penMask |= PenMask.TiltY;
            }

            var pointerFlags = PenPointerFlags.Update;
            if (inRange)
            {
                pointerFlags |= PenPointerFlags.InRange;
            }

            if (inContact)
            {
                pointerFlags |= PenPointerFlags.InContact;
            }

            if (penFlags.HasFlag(PenFlags.Barrel))
            {
                pointerFlags |= PenPointerFlags.FirstButton;
            }

            sample = PenSample.Create(
                unchecked((uint)deviceHandle.GetHashCode()),
                DateTimeOffset.UtcNow,
                position,
                inRange,
                inContact,
                pressure,
                xTilt,
                yTilt,
                rotation: 0,
                penFlags,
                penMask,
                pointerFlags);
            return true;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(_preparsedData);
        }

        private bool TryGetUsageValue(
            ushort usagePage,
            ushort usage,
            byte[] report,
            out uint value,
            out HidPValueCaps valueCap)
        {
            foreach (var cap in _valueCaps)
            {
                if (cap.UsagePage != usagePage || !cap.ContainsUsage(usage))
                {
                    continue;
                }

                var result = NativeMethods.HidP_GetUsageValue(
                    HidPReportType.Input,
                    usagePage,
                    cap.LinkCollection,
                    usage,
                    out value,
                    _preparsedData,
                    report,
                    (uint)report.Length);
                if (result == NativeMethods.HIDP_STATUS_SUCCESS)
                {
                    valueCap = cap;
                    return true;
                }
            }

            value = 0;
            valueCap = default;
            return false;
        }

        private bool TryGetScaledUsageValue(
            ushort usagePage,
            ushort usage,
            byte[] report,
            out int value)
        {
            foreach (var cap in _valueCaps)
            {
                if (cap.UsagePage != usagePage || !cap.ContainsUsage(usage))
                {
                    continue;
                }

                var result = NativeMethods.HidP_GetScaledUsageValue(
                    HidPReportType.Input,
                    usagePage,
                    cap.LinkCollection,
                    usage,
                    out value,
                    _preparsedData,
                    report,
                    (uint)report.Length);
                if (result == NativeMethods.HIDP_STATUS_SUCCESS)
                {
                    return true;
                }
            }

            value = 0;
            return false;
        }

        private ushort[] GetActiveDigitizerUsages(byte[] report)
        {
            var usages = new ushort[64];
            var usageLength = (uint)usages.Length;
            var result = NativeMethods.HidP_GetUsages(
                HidPReportType.Input,
                UsagePageDigitizer,
                linkCollection: 0,
                usages,
                ref usageLength,
                _preparsedData,
                report,
                (uint)report.Length);
            if (result != NativeMethods.HIDP_STATUS_SUCCESS || usageLength == 0)
            {
                return [];
            }

            if (usageLength == usages.Length)
            {
                return usages;
            }

            var activeUsages = new ushort[usageLength];
            Array.Copy(usages, activeUsages, activeUsages.Length);
            return activeUsages;
        }

        private static HidPValueCaps[] GetValueCaps(IntPtr preparsedData, HidPCaps caps)
        {
            if (caps.NumberInputValueCaps == 0)
            {
                return [];
            }

            var valueCapsLength = caps.NumberInputValueCaps;
            var valueCaps = new HidPValueCaps[valueCapsLength];
            var result = NativeMethods.HidP_GetValueCaps(
                HidPReportType.Input,
                valueCaps,
                ref valueCapsLength,
                preparsedData);
            if (result != NativeMethods.HIDP_STATUS_SUCCESS)
            {
                return [];
            }

            if (valueCapsLength == valueCaps.Length)
            {
                return valueCaps;
            }

            Array.Resize(ref valueCaps, valueCapsLength);
            return valueCaps;
        }

        private static bool ContainsUsage(ushort[] usages, ushort usage)
        {
            return Array.IndexOf(usages, usage) >= 0;
        }

        private static uint NormalizePressure(uint value, HidPValueCaps cap)
        {
            var min = cap.LogicalMin;
            var max = cap.LogicalMax;
            if (max <= min)
            {
                min = 0;
                max = (int)PenSample.MaxPointerPressure;
            }

            var normalized = ((double)value - min) / (max - min);
            var scaled = Math.Round(Math.Clamp(normalized, 0.0, 1.0) * PenSample.MaxPointerPressure);
            return (uint)scaled;
        }

        private static int ToInt32(uint value, HidPValueCaps cap)
        {
            if (cap.LogicalMin < 0)
            {
                return unchecked((int)value);
            }

            return value > int.MaxValue ? int.MaxValue : (int)value;
        }
    }
}
