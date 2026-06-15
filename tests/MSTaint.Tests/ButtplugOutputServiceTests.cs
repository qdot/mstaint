namespace MSTaint.Tests;

public sealed class ButtplugOutputServiceTests
{
    [Fact]
    public void ResolveDeviceNameUsesDisplayNameWhenPresent()
    {
        var name = ButtplugOutputService.ResolveDeviceName("User Name", "Device Name");

        Assert.Equal("User Name", name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveDeviceNameFallsBackToDeviceNameWhenDisplayNameIsMissing(string? displayName)
    {
        var name = ButtplugOutputService.ResolveDeviceName(displayName, "Device Name");

        Assert.Equal("Device Name", name);
    }
}
