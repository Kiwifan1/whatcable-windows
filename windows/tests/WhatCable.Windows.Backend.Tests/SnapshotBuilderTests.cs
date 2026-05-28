using WhatCable.Core;
using WhatCable.Windows.Backend.Power;
using WhatCable.Windows.Backend.Usb;
using Xunit;

namespace WhatCable.Windows.Backend.Tests;

public sealed class SnapshotBuilderTests
{
    private static SnapshotBuilder BuildWithFixtures() => new(
        new FakeUsbEnumerator(new UsbHostController
        {
            Name = "USB Host Controller 1",
            Devices = new[]
            {
                new UsbDeviceInfo
                {
                    Name = "Keyboard",
                    VendorId = 0x046D,
                    ProductId = 0xC31C,
                    Speed = UsbSpeed.Full,
                    PortNumber = 1,
                },
            },
        }),
        new FakeSystemPowerSource(new SystemPowerInfo
        {
            AcOnline = true,
            BatteryPresent = true,
            Charging = true,
            BatteryPercent = 90,
        }));

    [Fact]
    public void Build_PopulatesVersionAdapterAndPorts()
    {
        var snapshot = BuildWithFixtures().Build();

        Assert.Equal(SnapshotBuilder.SchemaVersion, snapshot.Version);
        Assert.False(snapshot.IsDesktopMac);
        Assert.NotNull(snapshot.Adapter);
        Assert.Equal("AC", snapshot.Adapter!.Source);
        Assert.Single(snapshot.Ports);
    }

    [Fact]
    public void Build_RendersValidJsonWithUnavailableReason()
    {
        var snapshot = BuildWithFixtures().Build();

        var json = JsonFormatter.Render(snapshot);

        Assert.Contains("\"unavailable_reason\": \"ucsi_not_supported\"", json);

        // Round-trips through the schema parser without loss.
        var reparsed = JsonFormatter.Parse(json);
        Assert.Equal("ucsi_not_supported", reparsed.Ports[0].UnavailableReason);
    }
}
