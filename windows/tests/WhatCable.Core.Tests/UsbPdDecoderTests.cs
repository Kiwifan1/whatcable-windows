using Xunit;

namespace WhatCable.Core.Tests;

public sealed class UsbPdDecoderTests
{
    [Fact]
    public void DecodeIdHeader_PassiveCableVector()
    {
        var vdo = (uint)((3 << 27) | 0x05AC);
        var header = UsbPdDecoder.DecodeIdHeader(vdo);

        Assert.Equal(ProductType.PassiveCable, header.UfpProductType);
        Assert.Equal(0x05AC, header.VendorId);
    }

    [Fact]
    public void DecodeCableVdo_DecodesNominalPassiveCable()
    {
        var vdo = (uint)(0b011 | (1 << 4) | (2 << 5) | (1 << 13));
        var cable = UsbPdDecoder.DecodeCableVdo(vdo, isActive: false);

        Assert.Equal(CableSpeed.Usb4Gen3, cable.Speed);
        Assert.Equal(CableCurrent.FiveAmp, cable.Current);
        Assert.Equal(100, cable.MaxWatts);
        Assert.Empty(cable.DecodeWarnings);
    }

    [Fact]
    public void DecodeCableVdo_FlagsReservedSpeed()
    {
        var vdo = (uint)(0b110 | (1 << 13));
        var cable = UsbPdDecoder.DecodeCableVdo(vdo, isActive: false);
        Assert.Contains(CableDecodeWarning.ReservedSpeedEncoding, cable.DecodeWarnings);
    }

    [Fact]
    public void DecodeActiveCableVdo2_ProtocolBitsInverted()
    {
        var vdo = (uint)0;
        var decoded = UsbPdDecoder.DecodeActiveCableVdo2(vdo);
        Assert.True(decoded.Usb4Supported);
        Assert.True(decoded.Usb2Supported);
        Assert.True(decoded.Usb32Supported);
    }

    [Fact]
    public void VdoFromData_ReadsLittleEndian()
    {
        var data = new byte[] { 0xEF, 0xBE, 0xAD, 0xDE };
        Assert.Equal(0xDEADBEEFu, UsbPdDecoder.VdoFromData(data));
    }
}
