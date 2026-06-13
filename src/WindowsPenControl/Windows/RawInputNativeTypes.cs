using System.Runtime.InteropServices;

namespace WindowsPenControl.Windows;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RawInputDevice
{
    public RawInputDevice(ushort usagePage, ushort usage, uint flags, IntPtr target)
    {
        UsagePage = usagePage;
        Usage = usage;
        Flags = flags;
        Target = target;
    }

    public readonly ushort UsagePage;
    public readonly ushort Usage;
    public readonly uint Flags;
    public readonly IntPtr Target;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RawInputHeader
{
    public readonly uint Type;
    public readonly uint Size;
    public readonly IntPtr Device;
    public readonly IntPtr WParam;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HidPCaps
{
    public readonly ushort Usage;
    public readonly ushort UsagePage;
    public readonly ushort InputReportByteLength;
    public readonly ushort OutputReportByteLength;
    public readonly ushort FeatureReportByteLength;
    private readonly ushort _reserved0;
    private readonly ushort _reserved1;
    private readonly ushort _reserved2;
    private readonly ushort _reserved3;
    private readonly ushort _reserved4;
    private readonly ushort _reserved5;
    private readonly ushort _reserved6;
    private readonly ushort _reserved7;
    private readonly ushort _reserved8;
    private readonly ushort _reserved9;
    private readonly ushort _reserved10;
    private readonly ushort _reserved11;
    private readonly ushort _reserved12;
    private readonly ushort _reserved13;
    private readonly ushort _reserved14;
    private readonly ushort _reserved15;
    private readonly ushort _reserved16;
    public readonly ushort NumberLinkCollectionNodes;
    public readonly ushort NumberInputButtonCaps;
    public readonly ushort NumberInputValueCaps;
    public readonly ushort NumberInputDataIndices;
    public readonly ushort NumberOutputButtonCaps;
    public readonly ushort NumberOutputValueCaps;
    public readonly ushort NumberOutputDataIndices;
    public readonly ushort NumberFeatureButtonCaps;
    public readonly ushort NumberFeatureValueCaps;
    public readonly ushort NumberFeatureDataIndices;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HidPValueCaps
{
    public readonly ushort UsagePage;
    public readonly byte ReportId;
    public readonly byte IsAlias;
    public readonly ushort BitField;
    public readonly ushort LinkCollection;
    public readonly ushort LinkUsage;
    public readonly ushort LinkUsagePage;
    public readonly byte IsRange;
    public readonly byte IsStringRange;
    public readonly byte IsDesignatorRange;
    public readonly byte IsAbsolute;
    public readonly byte HasNull;
    public readonly byte Reserved;
    public readonly ushort BitSize;
    public readonly ushort ReportCount;
    private readonly ushort _reserved0;
    private readonly ushort _reserved1;
    private readonly ushort _reserved2;
    private readonly ushort _reserved3;
    private readonly ushort _reserved4;
    public readonly uint UnitsExp;
    public readonly uint Units;
    public readonly int LogicalMin;
    public readonly int LogicalMax;
    public readonly int PhysicalMin;
    public readonly int PhysicalMax;
    public readonly ushort UsageMin;
    public readonly ushort UsageMax;
    public readonly ushort StringMin;
    public readonly ushort StringMax;
    public readonly ushort DesignatorMin;
    public readonly ushort DesignatorMax;
    public readonly ushort DataIndexMin;
    public readonly ushort DataIndexMax;

    public bool ContainsUsage(ushort usage)
    {
        return IsRange == 0
            ? UsageMin == usage
            : usage >= UsageMin && usage <= UsageMax;
    }
}

internal enum HidPReportType
{
    Input = 0,
    Output = 1,
    Feature = 2,
}
