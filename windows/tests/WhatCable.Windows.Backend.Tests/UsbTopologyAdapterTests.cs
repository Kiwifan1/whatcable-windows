using WhatCable.Windows.Backend.Usb;
using Xunit;

namespace WhatCable.Windows.Backend.Tests;

public sealed class UsbTopologyAdapterTests
{
    private static UsbHostController RecordedTree() => new()
    {
        Name = "USB Host Controller 1",
        InterfacePath = @"\\?\usb#controller1",
        Devices = new[]
        {
            new UsbDeviceInfo
            {
                Name = "Dock",
                VendorId = 0x05AC,
                ProductId = 0x1234,
                Speed = UsbSpeed.SuperSpeedPlus,
                PortNumber = 1,
                IsHub = true,
                Children = new[]
                {
                    new UsbDeviceInfo
                    {
                        Name = "SSD",
                        VendorId = 0xDEAD,
                        ProductId = 0x0001,
                        Speed = UsbSpeed.SuperSpeed,
                        PortNumber = 3,
                    },
                },
            },
            new UsbDeviceInfo
            {
                Name = "Keyboard",
                VendorId = 0x046D,
                ProductId = 0xC31C,
                Speed = UsbSpeed.Full,
                PortNumber = 2,
            },
        },
    };

    [Fact]
    public void GetPorts_BuildsDeviceTreeWithNegotiatedSpeeds()
    {
        var adapter = new UsbTopologyAdapter(new FakeUsbEnumerator(RecordedTree()));

        var ports = adapter.GetPorts();

        var port = Assert.Single(ports);
        Assert.True(port.ConnectionActive);
        Assert.Equal("UsbHostController", port.ClassName);
        Assert.Equal("3 devices", port.Subtitle);

        var root = port.Device!;
        Assert.Equal(2, root.Children!.Count);

        var dock = root.Children![0];
        Assert.Equal("Dock", dock.Name);
        Assert.Equal("USB 3.2 Gen 2 (10 Gbps)", dock.Speed);
        Assert.Equal("Apple Inc.", dock.VendorName);

        var ssd = Assert.Single(dock.Children!);
        Assert.Equal("USB 3.2 Gen 1 (5 Gbps)", ssd.Speed);
        Assert.Equal("1.1.3", ssd.LocationId);
    }

    [Fact]
    public void GetPorts_MarksPdAndEMarkerUnavailableWithoutUcsi()
    {
        var adapter = new UsbTopologyAdapter(new FakeUsbEnumerator(RecordedTree()));

        var port = Assert.Single(adapter.GetPorts());

        Assert.Equal("ucsi_not_supported", port.UnavailableReason);
        Assert.False(port.PdCapable);
        Assert.Null(port.Cable);
        Assert.Empty(port.PowerSources);
    }

    [Fact]
    public void GetPorts_WithNoControllers_ReturnsEmpty()
    {
        var adapter = new UsbTopologyAdapter(new FakeUsbEnumerator());

        Assert.Empty(adapter.GetPorts());
    }

    [Fact]
    public void GetPorts_WithEmptyController_ReportsAvailableButInactive()
    {
        var controller = new UsbHostController { Name = "USB Host Controller 1" };
        var adapter = new UsbTopologyAdapter(new FakeUsbEnumerator(controller));

        var port = Assert.Single(adapter.GetPorts());

        Assert.False(port.ConnectionActive);
        Assert.Equal("available", port.Status);
        Assert.Equal("0 devices", port.Subtitle);
    }
}
