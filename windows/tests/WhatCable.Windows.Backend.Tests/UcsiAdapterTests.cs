using WhatCable.Core;
using WhatCable.Windows.Backend;
using WhatCable.Windows.Backend.Power;
using WhatCable.Windows.Backend.Ucsi;
using WhatCable.Windows.Backend.Usb;
using Xunit;

namespace WhatCable.Windows.Backend.Tests;

public sealed class UcsiAdapterTests
{
    // VDOs for a passive 100W / USB4 Gen3 cable e-marker (SOP').
    private const uint CableIdHeader = (3u << 27) | 0x05AC;       // PassiveCable, Apple VID
    private const uint CableVdoRaw = 0b011u | (1u << 4) | (2u << 5) | (1u << 13);

    private static FakeUcsiTransport BuildFixture()
    {
        var fixed5v3a = ((uint)100 << 10) | 300;
        var fixed20v5a = ((uint)400 << 10) | 500;

        var status = new UcsiBitPacker()
            .Write(0, 16).Write(3, 3).Write(1, 1).Write(0, 1)
            .Write(0, 8).Write(1, 3).Write(0x20000000, 32) // PD, connected, sinking, DFP partner, RDO obj-pos 2
            .ToArray();

        var cable = new UcsiBitPacker()
            .Write(0x0008, 16).Write(60, 8).Write(1, 1).Write(0, 1)
            .Write(0, 1).Write(2, 2).Write(1, 1).Write(2, 2).Write(2, 4)
            .ToArray();

        return new FakeUcsiTransport { Version = 0x0200, ConnectorCount = 1, LiquidDetected = true }
            .Set("cap:1", new byte[] { 0xE4, 0x03 })
            .Set("status:1", status)
            .Set("pdos:1:p:src", UcsiTestBytes.Words(fixed5v3a, fixed20v5a))
            .Set("pdos:1:p:snk", UcsiTestBytes.Words(fixed5v3a))
            .Set("cable:1", cable)
            .Set("altmodes:1:Connector", UcsiTestBytes.AlternateModes((0xFF01, 0x1C05), (0x8087, 0x0001)))
            .Set("cam:1", new byte[] { 0x01 })
            .Set("currentcam:1", new byte[] { 0x01 })
            .Set("pd:1:SopPrime:DiscoverIdentity", UcsiTestBytes.Words(CableIdHeader, 1, 0x12345678, CableVdoRaw))
            .Set("pd:1:Connector:DiscoverIdentity", UcsiTestBytes.Words((2u << 27) | 0x1234, 1, 0x55667788))
            .Set("pd:1:Connector:DiscoverModes", UcsiTestBytes.Words(0x04030201, 0x08070605));
    }

    [Fact]
    public void GetSystem_PopulatesConnectorFields()
    {
        var system = new UcsiAdapter(BuildFixture()).GetSystem();

        Assert.True(system.Available);
        Assert.Equal("2.0", system.UcsiVersion);
        var connector = Assert.Single(system.Connectors);

        Assert.True(connector.Capability!.AlternateModes);
        Assert.True(connector.Status!.Connected);
        Assert.Equal(UcsiPowerOperationMode.PowerDelivery, connector.Status.PowerOperationMode);
        Assert.Equal(2, connector.SourcePdos.Count);
        Assert.Single(connector.SinkPdos);
        Assert.NotNull(connector.CableProperty);
        Assert.Equal(CableSpeed.Usb4Gen3, connector.CableVdo!.Speed);
        Assert.Equal(CableCurrent.FiveAmp, connector.CableVdo.Current);
        Assert.Equal(2, connector.AlternateModes.Count);
        Assert.True(connector.AlternateModes[0].Active);   // DisplayPort active per current CAM index
        Assert.False(connector.AlternateModes[1].Active);
        Assert.Equal(0x1234, connector.PartnerIdentity!.Header!.VendorId);
        Assert.Equal(2, connector.PartnerModes!.Modes.Count);
        Assert.True(connector.LiquidDetected);
    }

    [Fact]
    public void GetSystem_SetsAvailabilityFlags()
    {
        var connector = new UcsiAdapter(BuildFixture()).GetSystem().Connectors[0];
        var available = connector.Available;

        Assert.True(available.Capability);
        Assert.True(available.Status);
        Assert.True(available.SourcePdos);
        Assert.True(available.SinkPdos);
        Assert.True(available.CableProperty);
        Assert.True(available.CableIdentity);
        Assert.True(available.PartnerIdentity);
        Assert.True(available.AlternateModes);
        Assert.True(available.LiquidDetection);
    }

    [Fact]
    public void GetSystem_GatesUcsi20FeaturesOnVersion()
    {
        var fixture = BuildFixture();
        fixture.Version = 0x0120; // UCSI 1.2: no PD message round-trip or liquid detection.

        var connector = new UcsiAdapter(fixture).GetSystem().Connectors[0];

        Assert.Null(connector.CableIdentity);
        Assert.Null(connector.PartnerIdentity);
        Assert.Null(connector.LiquidDetected);
        Assert.False(connector.Available.LiquidDetection);
        // Non-2.0 fields still decode.
        Assert.NotNull(connector.CableProperty);
        Assert.Equal(2, connector.SourcePdos.Count);
    }

    [Fact]
    public void GetSystem_UnavailableTransportReturnsEmpty()
    {
        var system = new UcsiAdapter(new FakeUcsiTransport { IsAvailable = false }).GetSystem();
        Assert.False(system.Available);
        Assert.Empty(system.Connectors);
    }

    [Fact]
    public void GetPorts_MapsConnectorToSchemaPort()
    {
        var ports = new UcsiAdapter(BuildFixture()).GetPorts();

        var port = Assert.Single(ports);
        Assert.Equal("UsbCConnector", port.ClassName);
        Assert.Equal("USB-C", port.Type);
        Assert.True(port.PdCapable);
        Assert.True(port.ConnectionActive);
        Assert.Null(port.UnavailableReason);
        Assert.NotNull(port.Ucsi);

        var profile = Assert.Single(port.PowerSources);
        Assert.Equal(100, profile.MaxPowerW);          // 20V * 5A
        Assert.Equal(2, profile.Options.Count);
        Assert.NotNull(profile.Negotiated);              // RDO object position 2 -> 20V/5A PDO
        Assert.Equal(20000, profile.Negotiated!.MaxVoltageMv);

        Assert.NotNull(port.Cable);
        Assert.Equal("USB4 Gen 3 (40 Gbps)", port.Cable!.Speed);
        Assert.Equal("Passive", port.Cable.Type);
        Assert.Equal(0x05AC, port.Cable.VendorId);
    }

    [Fact]
    public void SnapshotBuilder_IncludesUcsiAndUsbPorts()
    {
        var usb = new FakeUsbEnumerator(new UsbHostController
        {
            Name = "USB Host Controller 1",
            Devices = new[] { new UsbDeviceInfo { Name = "Keyboard", PortNumber = 1, Speed = UsbSpeed.Full } },
        });
        var power = new FakeSystemPowerSource(new SystemPowerInfo { AcOnline = true });

        var snapshot = new SnapshotBuilder(usb, power, BuildFixture()).Build();
        var json = JsonFormatter.Render(snapshot);

        // UCSI connector port comes first, followed by the USB host controller port.
        Assert.Equal(2, snapshot.Ports.Count);
        Assert.Equal("UsbCConnector", snapshot.Ports[0].ClassName);
        Assert.Equal("UsbHostController", snapshot.Ports[1].ClassName);
        Assert.Contains("\"ucsi_version\": \"2.0\"", json);
        Assert.Contains("\"liquidDetected\": true", json);

        // Round-trips through the schema parser without loss.
        var reparsed = JsonFormatter.Parse(json);
        Assert.NotNull(reparsed.Ports[0].Ucsi);
    }

    [Fact]
    public void SnapshotBuilder_WithoutUcsiOmitsConnectorPorts()
    {
        var usb = new FakeUsbEnumerator(new UsbHostController { Name = "USB Host Controller 1" });
        var power = new FakeSystemPowerSource(new SystemPowerInfo { AcOnline = true });

        var snapshot = new SnapshotBuilder(usb, power).Build();

        Assert.All(snapshot.Ports, p => Assert.Equal("UsbHostController", p.ClassName));
    }
}
