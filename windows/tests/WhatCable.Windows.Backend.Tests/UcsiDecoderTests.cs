using WhatCable.Core;
using WhatCable.Windows.Backend.Ucsi;
using Xunit;

namespace WhatCable.Windows.Backend.Tests;

public sealed class UcsiDecoderTests
{
    [Fact]
    public void FormatVersion_RendersBcd()
    {
        Assert.Equal("2.0", UcsiDecoder.FormatVersion(0x0200));
        Assert.Equal("1.2", UcsiDecoder.FormatVersion(0x0120));
    }

    [Fact]
    public void DecodeConnectorCapability_DrpUsbAltMode()
    {
        // Operation mode = DRP | USB2 | USB3 | AltMode (0xE4); provider + consumer bits set.
        var data = new byte[] { 0xE4, 0x03 };

        var cap = UcsiDecoder.DecodeConnectorCapability(data);

        Assert.True(cap.Drp);
        Assert.True(cap.Usb2);
        Assert.True(cap.Usb3);
        Assert.True(cap.AlternateModes);
        Assert.False(cap.RpOnly);
        Assert.True(cap.Provider);
        Assert.True(cap.Consumer);
    }

    [Fact]
    public void DecodeConnectorStatus_PdContractSinking()
    {
        var data = new UcsiBitPacker()
            .Write(0, 16)              // status change
            .Write(3, 3)               // power op mode = PD
            .Write(1, 1)               // connected
            .Write(0, 1)               // sourcing = false (we are sinking)
            .Write(0, 8)               // partner flags
            .Write(1, 3)               // partner type = DFP
            .Write(0x20000000, 32)     // RDO: object position 2 (bits 30..28)
            .ToArray();

        var status = UcsiDecoder.DecodeConnectorStatus(data);

        Assert.True(status.Connected);
        Assert.False(status.Sourcing);
        Assert.Equal(UcsiPowerOperationMode.PowerDelivery, status.PowerOperationMode);
        Assert.Equal(UcsiPartnerType.Dfp, status.PartnerType);
        Assert.Equal(0x20000000u, status.RequestDataObject);
    }

    [Fact]
    public void DecodeCableProperty_PassiveTypeC()
    {
        var data = new UcsiBitPacker()
            .Write(0x0008, 16)   // bmSpeedSupported
            .Write(60, 8)        // current capability: 60 * 50mA = 3000mA
            .Write(1, 1)         // VBUS in cable
            .Write(0, 1)         // passive
            .Write(0, 1)         // directional
            .Write(2, 2)         // plug end type = Type-C
            .Write(1, 1)         // mode support
            .Write(2, 2)         // PD revision
            .Write(2, 4)         // latency
            .ToArray();

        var cable = UcsiDecoder.DecodeCableProperty(data);

        Assert.Equal(3000, cable.CurrentCapabilityMa);
        Assert.True(cable.VbusInCable);
        Assert.False(cable.Active);
        Assert.Equal(UcsiPlugEndType.TypeC, cable.PlugEndType);
        Assert.True(cable.ModeSupport);
        Assert.Equal(2, cable.PdRevision);
        Assert.Equal(2, cable.Latency);
    }

    [Fact]
    public void DecodeAlternateModes_SvidVdoRecords()
    {
        var data = UcsiTestBytes.AlternateModes((0xFF01, 0x00001C05), (0x8087, 0x00000001));

        var modes = UcsiDecoder.DecodeAlternateModes(data);

        Assert.Equal(2, modes.Count);
        Assert.Equal(0xFF01, modes[0].Svid);
        Assert.Equal("DisplayPort", modes[0].Name);
        Assert.Equal(0x00001C05u, modes[0].Vdo);
        Assert.Equal("Thunderbolt", modes[1].Name);
    }

    [Fact]
    public void DecodeAlternateModes_StopsAtZeroSvid()
    {
        var data = UcsiTestBytes.AlternateModes((0xFF01, 1), (0x0000, 0));
        var modes = UcsiDecoder.DecodeAlternateModes(data);
        Assert.Single(modes);
    }

    [Fact]
    public void DecodeCamSupported_Bitmap()
    {
        Assert.Equal(0x05u, UcsiDecoder.DecodeCamSupported(new byte[] { 0x05 }));
        Assert.Equal(0x0100u, UcsiDecoder.DecodeCamSupported(new byte[] { 0x00, 0x01 }));
    }

    [Fact]
    public void DecodePdos_DecodesAndSkipsPadding()
    {
        var fixed5v3a = ((uint)100 << 10) | 300;
        var fixed20v5a = ((uint)400 << 10) | 500;
        var data = UcsiTestBytes.Words(fixed5v3a, fixed20v5a, 0u);

        var pdos = UcsiDecoder.DecodePdos(data);

        Assert.Equal(2, pdos.Count);
        Assert.Equal("Fixed", pdos[0].Kind);
        Assert.Equal(5000, pdos[0].MinVoltageMv);
        Assert.Equal(20000, pdos[1].MaxVoltageMv);
    }
}
