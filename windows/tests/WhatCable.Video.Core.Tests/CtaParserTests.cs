using System.IO;
using System.Linq;
using Xunit;

namespace WhatCable.Video.Core.Tests;

public sealed class CtaParserTests
{
    // Sample CTA-861 extension block with HDMI 2.1 capabilities
    private static readonly byte[] SampleCtaBlock = new byte[128];

    private static byte[] LoadFixture(params string[] relativePath)
        => File.ReadAllBytes(Path.Combine(new[] { AppContext.BaseDirectory, "Fixtures" }.Concat(relativePath).ToArray()));

    static CtaParserTests()
    {
        // Initialize CTA block
        SampleCtaBlock[0] = 0x02; // CEA/CTA-861 tag
        SampleCtaBlock[1] = 0x03; // Revision 3
        SampleCtaBlock[2] = 0x20; // DTD starts at byte 32
        SampleCtaBlock[3] = 0xF0; // Flags: underscan, audio, YCbCr444, YCbCr422

        // Video Data Block (tag=2, length=5)
        int offset = 4;
        SampleCtaBlock[offset++] = 0x45; // Tag=2, Length=5
        SampleCtaBlock[offset++] = 0x10; // VIC 16: 1920x1080@60Hz
        SampleCtaBlock[offset++] = 0x04; // VIC 4: 1280x720@60Hz
        SampleCtaBlock[offset++] = 0xE1; // VIC 97: 3840x2160@60Hz (native, 0x80 | 97)
        SampleCtaBlock[offset++] = 0xF6; // VIC 118: 3840x2160@120Hz (native, 0x80 | 118)
        SampleCtaBlock[offset++] = 0x03; // VIC 3: 720x480@60Hz

        // Audio Data Block (tag=1, length=3)
        SampleCtaBlock[offset++] = 0x23; // Tag=1, Length=3
        SampleCtaBlock[offset++] = 0x09; // Format=LPCM, 2 channels
        SampleCtaBlock[offset++] = 0x07; // 32/44.1/48 kHz
        SampleCtaBlock[offset++] = 0x07; // 16/20/24 bit

        // Speaker Allocation Data Block (tag=4, length=3)
        SampleCtaBlock[offset++] = 0x83; // Tag=4, Length=3
        SampleCtaBlock[offset++] = 0x01; // FL/FR
        SampleCtaBlock[offset++] = 0x00;
        SampleCtaBlock[offset++] = 0x00;

        // HDMI VSDB (tag=3, length=8)
        SampleCtaBlock[offset++] = 0x68; // Tag=3, Length=8
        SampleCtaBlock[offset++] = 0x03; // IEEE OUI: 0x000C03 (HDMI 1.4)
        SampleCtaBlock[offset++] = 0x0C;
        SampleCtaBlock[offset++] = 0x00;
        SampleCtaBlock[offset++] = 0x10; // Physical address: 1.0.0.0
        SampleCtaBlock[offset++] = 0x00;
        SampleCtaBlock[offset++] = 0x78; // Deep color flags: 30/36/48-bit
        SampleCtaBlock[offset++] = 0x00;
        SampleCtaBlock[offset++] = 0x00;

        // Extended tag blocks
        // Colorimetry Data Block (extended tag=5, length=3 including extended tag)
        SampleCtaBlock[offset++] = 0xE3; // Tag=7, Length=3
        SampleCtaBlock[offset++] = 0x05; // Extended tag: Colorimetry
        SampleCtaBlock[offset++] = 0xE3; // xvYCC601, xvYCC709, sYCC601, AdobeYCC601, AdobeRGB, BT2020YCC, BT2020RGB
        SampleCtaBlock[offset++] = 0x00;

        // HDMI Forum VSDB (tag=3, length=13)
        SampleCtaBlock[offset++] = 0x6D; // Tag=3, Length=13
        SampleCtaBlock[offset++] = 0xD8; // IEEE OUI: 0xC45DD8 (HDMI Forum)
        SampleCtaBlock[offset++] = 0x5D;
        SampleCtaBlock[offset++] = 0xC4;
        SampleCtaBlock[offset++] = 0x01; // Version
        SampleCtaBlock[offset++] = 0x78; // Max TMDS character rate: 600 MHz
        SampleCtaBlock[offset++] = 0x80; // SCDC present
        SampleCtaBlock[offset++] = 0xE4; // Max FRL rate = 4, plus 4:2:0 deep color flags
        SampleCtaBlock[offset++] = 0x5A; // QMS, QFT, VRR, ALLM
        SampleCtaBlock[offset++] = 0x30; // VRR min = 48
        SampleCtaBlock[offset++] = 0x78; // VRR max = 120
        SampleCtaBlock[offset++] = 0x01; // DSC 1.2 support
        SampleCtaBlock[offset++] = 0x00;
        SampleCtaBlock[offset++] = 0x00;

        SampleCtaBlock[2] = (byte)offset;

        // Fill rest with zeros
        // Checksum at byte 127
        byte sum = 0;
        for (int i = 0; i < 127; i++)
        {
            sum += SampleCtaBlock[i];
        }
        SampleCtaBlock[127] = (byte)(256 - sum);
    }

    [Fact]
    public void Parse_ValidCtaBlock_ReturnsCtaInfo()
    {
        var cta = CtaParser.Parse(SampleCtaBlock);

        Assert.NotNull(cta);
        Assert.Equal(3, cta.Revision);
    }

    [Fact]
    public void Parse_ValidCtaBlock_ParsesFlags()
    {
        var cta = CtaParser.Parse(SampleCtaBlock);

        Assert.NotNull(cta);
        Assert.True(cta.UnderscanSupport);
        Assert.True(cta.BasicAudioSupport);
        Assert.True(cta.Ycbcr444Support);
        Assert.True(cta.Ycbcr422Support);
    }

    [Fact]
    public void Parse_ValidCtaBlock_ParsesVideoDescriptors()
    {
        var cta = CtaParser.Parse(SampleCtaBlock);

        Assert.NotNull(cta);
        Assert.NotEmpty(cta.VideoDescriptors);
        Assert.Equal(5, cta.VideoDescriptors.Count);

        // Check for 1920x1080@60Hz (VIC 16)
        var vic16 = cta.VideoDescriptors.FirstOrDefault(v => v.Vic == 16);
        Assert.NotNull(vic16);
        Assert.False(vic16.Native);
        Assert.NotNull(vic16.Mode);
        Assert.Equal(1920, vic16.Mode.WidthPx);
        Assert.Equal(1080, vic16.Mode.HeightPx);

        // Check for native 4K60 (VIC 97)
        var vic97 = cta.VideoDescriptors.FirstOrDefault(v => v.Vic == 97);
        Assert.NotNull(vic97);
        Assert.True(vic97.Native);
    }

    [Fact]
    public void Parse_ValidCtaBlock_ParsesAudioDescriptors()
    {
        var cta = CtaParser.Parse(SampleCtaBlock);

        Assert.NotNull(cta);
        Assert.NotEmpty(cta.AudioDescriptors);

        var lpcm = cta.AudioDescriptors.FirstOrDefault(a => a.Format == "LPCM");
        Assert.NotNull(lpcm);
        Assert.Equal(2, lpcm.MaxChannels);
        Assert.Contains(48, lpcm.SampleRatesKhz);
        Assert.NotNull(lpcm.BitDepths);
        Assert.Contains(24, lpcm.BitDepths);
    }

    [Fact]
    public void Parse_ValidCtaBlock_ParsesHdmiVsdb()
    {
        var cta = CtaParser.Parse(SampleCtaBlock);

        Assert.NotNull(cta);
        Assert.NotNull(cta.HdmiVsdb);
        Assert.Equal("1.0.0.0", cta.HdmiVsdb.SourcePhysicalAddress);
        Assert.True(cta.HdmiVsdb.Supports30bitDeepColor);
        Assert.True(cta.HdmiVsdb.Supports36bitDeepColor);
        Assert.True(cta.HdmiVsdb.Supports48bitDeepColor);
    }

    [Fact]
    public void Parse_ValidCtaBlock_ParsesSpeakerAllocation()
    {
        var cta = CtaParser.Parse(SampleCtaBlock);

        Assert.NotNull(cta);
        Assert.NotNull(cta.SpeakerAllocation);
        Assert.True(cta.SpeakerAllocation.FrontLeftRight);
    }

    [Fact]
    public void Parse_ValidCtaBlock_ParsesColorimetry()
    {
        var cta = CtaParser.Parse(SampleCtaBlock);

        Assert.NotNull(cta);
        Assert.NotNull(cta.Colorimetry);
        Assert.True(cta.Colorimetry.Xvycc601);
        Assert.True(cta.Colorimetry.Xvycc709);
        Assert.True(cta.Colorimetry.Bt2020Rgb);
    }

    [Fact]
    public void Parse_ValidCtaBlock_ParsesHdmiForumVsdb()
    {
        var cta = CtaParser.Parse(SampleCtaBlock);

        Assert.NotNull(cta);
        Assert.NotNull(cta.HdmiForumVsdb);
        Assert.Equal(1, cta.HdmiForumVsdb.Version);
        Assert.Equal(600, cta.HdmiForumVsdb.MaxTmdsCharacterRateMhz);
        Assert.Equal(4, cta.HdmiForumVsdb.MaxFrlRate);
        Assert.True(cta.HdmiForumVsdb.SupportsAllm);
        Assert.True(cta.HdmiForumVsdb.SupportsVrr);
        Assert.True(cta.HdmiForumVsdb.SupportsQms);
        Assert.True(cta.HdmiForumVsdb.SupportsQft);
        Assert.True(cta.HdmiForumVsdb.SupportsDsc);
    }

    [Fact]
    public void Parse_NullBlock_ReturnsNull()
    {
        var cta = CtaParser.Parse(null);
        Assert.Null(cta);
    }

    [Fact]
    public void Parse_TooShortBlock_ReturnsNull()
    {
        var cta = CtaParser.Parse(LoadFixture("cta", "truncated-extension.bin"));
        Assert.Null(cta);
    }

    [Fact]
    public void Parse_InvalidTag_ReturnsNull()
    {
        var invalidBlock = new byte[128];
        invalidBlock[0] = 0xFF; // Invalid tag

        var cta = CtaParser.Parse(invalidBlock);
        Assert.Null(cta);
    }
}
