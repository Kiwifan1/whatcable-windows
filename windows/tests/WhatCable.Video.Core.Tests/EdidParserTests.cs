using System;
using System.Linq;
using Xunit;

namespace WhatCable.Video.Core.Tests;

public sealed class EdidParserTests
{
    // Sample EDID header + basic structure for a 1920x1080@60Hz display
    private static readonly byte[] Sample1080p60Edid = new byte[]
    {
        // Header (bytes 0-7)
        0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00,
        // Manufacturer ID (bytes 8-9): "DEL" (Dell)
        0x10, 0xAC,
        // Product code (bytes 10-11)
        0x6B, 0x40,
        // Serial number (bytes 12-15)
        0x4C, 0x35, 0x39, 0x38,
        // Week/year (bytes 16-17): Week 32, Year 2020
        0x20, 0x1E,
        // EDID version (bytes 18-19): 1.4
        0x01, 0x04,
        // Video input definition (byte 20): Digital
        0xA5,
        // Screen size (bytes 21-22): 52cm x 29cm
        0x34, 0x1D,
        // Gamma (byte 23)
        0x78,
        // Features (byte 24)
        0x3A,
        // Color characteristics (bytes 25-34)
        0xEE, 0x95, 0xA3, 0x54, 0x4C, 0x99, 0x26, 0x0F, 0x50, 0x54,
        // Established timings (bytes 35-37)
        0x21, 0x08, 0x00,
        // Standard timings (bytes 38-53)
        0x81, 0xC0, 0x81, 0x80, 0x95, 0x00, 0xA9, 0x40,
        0xB3, 0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
        // Detailed timing descriptors (4 x 18 bytes = bytes 54-125)
        // Descriptor 1: 1920x1080@60Hz
        0x02, 0x3A, 0x80, 0x18, 0x71, 0x38, 0x2D, 0x40,
        0x58, 0x2C, 0x45, 0x00, 0x09, 0x25, 0x21, 0x00,
        0x00, 0x1E,
        // Descriptor 2: Display name
        0x00, 0x00, 0x00, 0xFC, 0x00, 0x44, 0x45, 0x4C,
        0x4C, 0x20, 0x55, 0x32, 0x34, 0x31, 0x32, 0x4D,
        0x0A, 0x20,
        // Descriptor 3: Display range limits
        0x00, 0x00, 0x00, 0xFD, 0x00, 0x38, 0x4C, 0x1E,
        0x53, 0x11, 0x00, 0x0A, 0x20, 0x20, 0x20, 0x20,
        0x20, 0x20,
        // Descriptor 4: Serial number
        0x00, 0x00, 0x00, 0xFF, 0x00, 0x35, 0x39, 0x38,
        0x4C, 0x33, 0x0A, 0x20, 0x20, 0x20, 0x20, 0x20,
        0x20, 0x20,
        // Extension count (byte 126)
        0x01,
        // Checksum (byte 127)
        0x00
    };

    static EdidParserTests()
    {
        // Calculate correct checksum for sample EDID
        CalculateChecksum(Sample1080p60Edid);
    }

    private static void CalculateChecksum(byte[] edid)
    {
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
        var edid = EdidParser.Parse(Sample1080p60Edid);

        Assert.NotNull(edid);
        Assert.Equal("1.4", edid.Version);
        Assert.Equal("DEL", edid.Manufacturer);
        Assert.Equal(0x406B, edid.ProductCode);
    }

    [Fact]
    public void Parse_ValidEdid_ParsesDisplayName()
    {
        var edid = EdidParser.Parse(Sample1080p60Edid);

        Assert.NotNull(edid);
        Assert.Equal("DELL U2412M", edid.DisplayName);
    }

    [Fact]
    public void Parse_ValidEdid_ParsesManufactureInfo()
    {
        var edid = EdidParser.Parse(Sample1080p60Edid);

        Assert.NotNull(edid);
        Assert.Equal(32, edid.ManufactureWeek);
        Assert.Equal(2020, edid.ManufactureYear);
    }

    [Fact]
    public void Parse_ValidEdid_ParsesScreenSize()
    {
        var edid = EdidParser.Parse(Sample1080p60Edid);

        Assert.NotNull(edid);
        Assert.Equal(52, edid.WidthCm);
        Assert.Equal(29, edid.HeightCm);
    }

    [Fact]
    public void Parse_ValidEdid_ParsesDetailedTiming()
    {
        var edid = EdidParser.Parse(Sample1080p60Edid);

        Assert.NotNull(edid);
        Assert.NotNull(edid.NativeResolution);
        Assert.Equal(1920, edid.NativeResolution.WidthPx);
        Assert.Equal(1080, edid.NativeResolution.HeightPx);
        Assert.True(edid.NativeResolution.RefreshRateHz >= 59 && edid.NativeResolution.RefreshRateHz <= 61);
    }

    [Fact]
    public void Parse_ValidEdid_ParsesEstablishedTimings()
    {
        var edid = EdidParser.Parse(Sample1080p60Edid);

        Assert.NotNull(edid);
        Assert.NotEmpty(edid.SupportedModes);
        // Should have at least the established timings
        Assert.Contains(edid.SupportedModes, m => m.WidthPx == 640 && m.HeightPx == 480);
    }

    [Fact]
    public void Parse_ValidEdid_ReturnsExtensionCount()
    {
        var edid = EdidParser.Parse(Sample1080p60Edid);

        Assert.NotNull(edid);
        Assert.Equal(1, edid.ExtensionBlocks);
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
        var invalidEdid = new byte[128];
        Array.Copy(Sample1080p60Edid, invalidEdid, 128);
        invalidEdid[0] = 0xFF; // Corrupt header

        var edid = EdidParser.Parse(invalidEdid);
        Assert.Null(edid);
    }

    [Fact]
    public void Parse_InvalidChecksum_ReturnsNull()
    {
        var invalidEdid = new byte[128];
        Array.Copy(Sample1080p60Edid, invalidEdid, 128);
        invalidEdid[127] = 0xFF; // Corrupt checksum

        var edid = EdidParser.Parse(invalidEdid);
        Assert.Null(edid);
    }
}
