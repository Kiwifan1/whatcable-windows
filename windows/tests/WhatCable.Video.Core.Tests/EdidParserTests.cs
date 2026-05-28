using System.IO;
using System.Linq;
using Xunit;

namespace WhatCable.Video.Core.Tests;

public sealed class EdidParserTests
{
    private static byte[] LoadFixture(params string[] relativePath)
        => File.ReadAllBytes(Path.Combine(new[] { AppContext.BaseDirectory, "Fixtures" }.Concat(relativePath).ToArray()));

    private static void UpdateChecksum(byte[] edid)
    {
        edid[127] = 0;
        byte sum = 0;
        for (int i = 0; i < 127; i++)
        {
            sum += edid[i];
        }

        edid[127] = (byte)(256 - sum);
    }

    [Fact]
    public void Parse_ValidEdid_ReturnsEdidInfo()
    {
        var edid = EdidParser.Parse(LoadFixture("edid", "1080p60-hdmi14.bin"));

        Assert.NotNull(edid);
        Assert.Equal("1.4", edid.Version);
        Assert.Equal("DEL", edid.Manufacturer);
        Assert.Equal(0x406B, edid.ProductCode);
    }

    [Fact]
    public void Parse_ValidEdid_ParsesDisplayName()
    {
        var edid = EdidParser.Parse(LoadFixture("edid", "1080p60-hdmi14.bin"));

        Assert.NotNull(edid);
        Assert.Equal("DELL U2412M", edid.DisplayName);
    }

    [Fact]
    public void Parse_ValidEdid_ParsesManufactureInfo()
    {
        var edid = EdidParser.Parse(LoadFixture("edid", "1080p60-hdmi14.bin"));

        Assert.NotNull(edid);
        Assert.Equal(32, edid.ManufactureWeek);
        Assert.Equal(2020, edid.ManufactureYear);
    }

    [Fact]
    public void Parse_ValidEdid_ParsesScreenSize()
    {
        var edid = EdidParser.Parse(LoadFixture("edid", "1080p60-hdmi14.bin"));

        Assert.NotNull(edid);
        Assert.Equal(52, edid.WidthCm);
        Assert.Equal(29, edid.HeightCm);
    }

    [Fact]
    public void Parse_ValidEdid_ParsesDetailedTiming()
    {
        var edid = EdidParser.Parse(LoadFixture("edid", "1080p60-hdmi14.bin"));

        Assert.NotNull(edid);
        Assert.NotNull(edid.NativeResolution);
        Assert.Equal(1920, edid.NativeResolution.WidthPx);
        Assert.Equal(1080, edid.NativeResolution.HeightPx);
        Assert.True(edid.NativeResolution.RefreshRateHz >= 59 && edid.NativeResolution.RefreshRateHz <= 61);
    }

    [Fact]
    public void Parse_ValidEdid_ParsesEstablishedTimings()
    {
        var edid = EdidParser.Parse(LoadFixture("edid", "1080p60-hdmi14.bin"));

        Assert.NotNull(edid);
        Assert.NotEmpty(edid.SupportedModes);
        // Should have at least the established timings
        Assert.Contains(edid.SupportedModes, m => m.WidthPx == 640 && m.HeightPx == 480);
    }

    [Fact]
    public void Parse_ValidEdid_ReturnsExtensionCount()
    {
        var edid = EdidParser.Parse(LoadFixture("edid", "1080p60-hdmi14.bin"));

        Assert.NotNull(edid);
        Assert.Equal(0, edid.ExtensionBlocks);
    }

    [Fact]
    public void Parse_NullEdid_ReturnsNull()
    {
        var edid = EdidParser.Parse(null);
        Assert.Null(edid);
    }

    [Fact]
    public void Parse_TooShortEdid_ReturnsNull()
    {
        var edid = EdidParser.Parse(new byte[64]);
        Assert.Null(edid);
    }

    [Fact]
    public void Parse_InvalidHeader_ReturnsNull()
    {
        var edid = EdidParser.Parse(LoadFixture("edid", "malformed-header.bin"));
        Assert.Null(edid);
    }

    [Fact]
    public void Parse_InvalidChecksum_ReturnsNull()
    {
        var edid = EdidParser.Parse(LoadFixture("edid", "bad-checksum.bin"));
        Assert.Null(edid);
    }

    [Fact]
    public void Parse_FixtureWithCtaExtension_Parses4k60Mode()
    {
        var edid = EdidParser.Parse(LoadFixture("edid", "4k60-hdmi.bin"));

        Assert.NotNull(edid);
        Assert.NotNull(edid.CtaInfo);
        Assert.Contains(edid.CtaInfo!.VideoDescriptors, descriptor => descriptor.Vic == 97);
    }

    [Fact]
    public void Parse_FixtureWithHdmiForumVsdb_Parses4k120Capabilities()
    {
        var edid = EdidParser.Parse(LoadFixture("edid", "4k120-hdmi21.bin"));

        Assert.NotNull(edid);
        Assert.NotNull(edid.CtaInfo?.HdmiForumVsdb);
        Assert.Equal(4, edid.CtaInfo!.HdmiForumVsdb!.MaxFrlRate);
        Assert.True(edid.CtaInfo.HdmiForumVsdb.SupportsAllm);
        Assert.True(edid.CtaInfo.HdmiForumVsdb.SupportsVrr);
    }

    [Fact]
    public void Parse_FixtureWithDisplayIdExtension_ParsesUhbr20Support()
    {
        var edid = EdidParser.Parse(LoadFixture("edid", "4k240-dp-uhbr20.bin"));

        Assert.NotNull(edid);
        Assert.NotNull(edid.DisplayIdInfo);
        Assert.Contains("UHBR20", edid.DisplayIdInfo!.UhbrSupport);
    }

    [Fact]
    public void Parse_InvalidManufacturerCode_ReturnsNullManufacturer()
    {
        var edidBytes = LoadFixture("edid", "1080p60-hdmi14.bin");
        edidBytes[8] = 0x00;
        edidBytes[9] = 0x00;
        UpdateChecksum(edidBytes);

        var edid = EdidParser.Parse(edidBytes);

        Assert.NotNull(edid);
        Assert.Null(edid!.Manufacturer);
    }
}
