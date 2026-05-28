using Xunit;

namespace WhatCable.Core.Tests;

public sealed class PdoDecoderTests
{
    [Fact]
    public void DecodeFixedPdo()
    {
        var raw = ((uint)100 << 10) | 300; // 5V, 3A
        var pdo = PdoDecoder.Decode(raw);

        Assert.Equal("Fixed", pdo.Kind);
        Assert.Equal(5000, pdo.MinVoltageMv);
        Assert.Equal(3000, pdo.MaxCurrentMa);
        Assert.False(pdo.DualRolePower);
        Assert.False(pdo.UsbSuspendSupported);
        Assert.False(pdo.UnconstrainedPower);
        Assert.Equal(100, pdo.PeakCurrent);
    }

    [Fact]
    public void DecodeFixedPdo_SourceCapabilityBits()
    {
        var raw = ((uint)1 << 29) | ((uint)1 << 28) | ((uint)1 << 27) | ((uint)2 << 20) | ((uint)100 << 10) | 300;
        var pdo = PdoDecoder.Decode(raw);

        Assert.True(pdo.DualRolePower);
        Assert.True(pdo.UsbSuspendSupported);
        Assert.True(pdo.UnconstrainedPower);
        Assert.Equal(150, pdo.PeakCurrent);
    }

    [Fact]
    public void DecodeVariablePdo()
    {
        var raw = ((uint)2 << 30) | ((uint)400 << 10) | (uint)(100) | ((uint)300 << 20);
        var pdo = PdoDecoder.Decode(raw);

        Assert.Equal("Variable", pdo.Kind);
        Assert.Equal(5000, pdo.MinVoltageMv);
        Assert.Equal(20000, pdo.MaxVoltageMv);
        Assert.Equal(3000, pdo.MaxCurrentMa);
    }

    [Fact]
    public void DecodeBatteryPdo()
    {
        var raw = ((uint)1 << 30) | ((uint)400 << 10) | (uint)100 | ((uint)240 << 20);
        var pdo = PdoDecoder.Decode(raw);

        Assert.Equal("Battery", pdo.Kind);
        Assert.Equal(5000, pdo.MinVoltageMv);
        Assert.Equal(20000, pdo.MaxVoltageMv);
        Assert.Equal(60000, pdo.MaxPowerMw);
    }

    [Fact]
    public void DecodePpsApdo()
    {
        var raw = ((uint)3 << 30) | ((uint)50 << 17) | ((uint)110 << 8) | 33;
        var pdo = PdoDecoder.Decode(raw);

        Assert.Equal("PpsApdo", pdo.Kind);
        Assert.Equal(3300, pdo.MinVoltageMv);
        Assert.Equal(11000, pdo.MaxVoltageMv);
        Assert.Equal(2500, pdo.MaxCurrentMa);
    }

    [Fact]
    public void DecodeEprAvs()
    {
        var raw = ((uint)3 << 30) | ((uint)1 << 28) | ((uint)140 << 17) | ((uint)50 << 8) | (uint)15;
        var pdo = PdoDecoder.Decode(raw);

        Assert.Equal("EprAvs", pdo.Kind);
        Assert.Equal(15000, pdo.MinVoltageMv);
        Assert.Equal(50000, pdo.MaxVoltageMv);
        Assert.Equal(140000, pdo.MaxPowerMw);
    }
}
