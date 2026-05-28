using Xunit;

namespace WhatCable.Core.Tests;

public sealed class DiscoverPayloadDecoderTests
{
    [Fact]
    public void DecodeDiscoverIdentityPayload()
    {
        var bytes = new byte[]
        {
            0xAC, 0x05, 0x00, 0x18,
            0x45, 0x23, 0x01, 0x00,
            0x34, 0x12, 0x78, 0x56
        };

        var payload = DiscoverPayloadDecoder.DecodeDiscoverIdentity(bytes);
        Assert.NotNull(payload.Header);
        Assert.Equal(0x05AC, payload.Header!.VendorId);
        Assert.Equal(0x00012345u, payload.CertStat!.Xid);
        Assert.Equal(0x1234, payload.Product!.ProductId);
        Assert.Equal(0x5678, payload.Product.BcdDevice);
    }

    [Fact]
    public void DecodeDiscoverSvidsPayload()
    {
        // VDO[0] = 0xFFD00001: SVID0=0xFFD0 in bits 31..16, SVID1=0x0001 in bits 15..0
        // VDO[1] = 0x00000000: zero high-half terminates the list
        var bytes = new byte[] { 0x01, 0x00, 0xD0, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x78, 0x56, 0x9A, 0xBC };
        var payload = DiscoverPayloadDecoder.DecodeDiscoverSvids(bytes);
        Assert.Equal(new[] { 0xFFD0, 0x0001 }, payload.Svids);
        Assert.Equal(3, payload.RawVdos.Count);
    }

    [Fact]
    public void DecodeDiscoverModesPayload()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var payload = DiscoverPayloadDecoder.DecodeDiscoverModes(bytes);
        Assert.Equal(2, payload.Modes.Count);
        Assert.Equal(0x04030201u, payload.Modes[0].Raw);
    }
}
