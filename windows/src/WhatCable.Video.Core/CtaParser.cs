using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatCable.Video.Core;

/// <summary>
/// CTA-861 (CEA-861) extension block parser for HDMI capabilities.
/// </summary>
public static class CtaParser
{
    private static readonly Dictionary<int, VideoMode> VicTable = InitializeVicTable();

    /// <summary>
    /// Parse CTA-861 extension block.
    /// </summary>
    public static CtaInfo? Parse(byte[] ctaBlock)
    {
        if (ctaBlock == null || ctaBlock.Length < 128)
            return null;

        byte tag = ctaBlock[0];
        if (tag != 0x02) // CEA/CTA-861 extension tag
            return null;

        byte revision = ctaBlock[1];
        byte dtdStart = ctaBlock[2];

        if (dtdStart > 127)
            return null;

        byte flags = ctaBlock[3];
        bool underscanSupport = (flags & 0x80) != 0;
        bool basicAudioSupport = (flags & 0x40) != 0;
        bool ycbcr444Support = (flags & 0x20) != 0;
        bool ycbcr422Support = (flags & 0x10) != 0;

        var videoDescriptors = new List<VideoDescriptor>();
        var audioDescriptors = new List<AudioDescriptor>();
        HdmiVsdb? hdmiVsdb = null;
        HdmiForumVsdb? hdmiForumVsdb = null;
        HdrStaticMetadata? hdrStaticMetadata = null;
        SpeakerAllocation? speakerAllocation = null;
        ColorimetryDataBlock? colorimetry = null;
        VideoCapabilityDataBlock? videoCapability = null;

        // Parse data blocks
        int offset = 4;
        while (offset < dtdStart && offset < 127)
        {
            byte header = ctaBlock[offset];
            byte blockTag = (byte)((header >> 5) & 0x07);
            byte blockLength = (byte)(header & 0x1F);

            if (offset + blockLength + 1 > dtdStart)
                break;

            byte[] blockData = ctaBlock.Skip(offset + 1).Take(blockLength).ToArray();

            switch (blockTag)
            {
                case 1: // Audio Data Block
                    ParseAudioDataBlock(blockData, audioDescriptors);
                    break;
                case 2: // Video Data Block
                    ParseVideoDataBlock(blockData, videoDescriptors);
                    break;
                case 3: // Vendor Specific Data Block
                    ParseVendorSpecificDataBlock(blockData, ref hdmiVsdb, ref hdmiForumVsdb);
                    break;
                case 4: // Speaker Allocation Data Block
                    speakerAllocation = ParseSpeakerAllocation(blockData);
                    break;
                case 7: // Extended Tag
                    if (blockLength > 0)
                    {
                        byte extendedTag = blockData[0];
                        byte[] extendedData = blockData.Skip(1).ToArray();
                        switch (extendedTag)
                        {
                            case 0: // Video Capability Data Block
                                videoCapability = ParseVideoCapabilityDataBlock(extendedData);
                                break;
                            case 5: // Colorimetry Data Block
                                colorimetry = ParseColorimetryDataBlock(extendedData);
                                break;
                            case 6: // HDR Static Metadata Data Block
                                hdrStaticMetadata = ParseHdrStaticMetadata(extendedData);
                                break;
                        }
                    }
                    break;
            }

            offset += blockLength + 1;
        }

        return new CtaInfo
        {
            Revision = revision,
            UnderscanSupport = underscanSupport,
            BasicAudioSupport = basicAudioSupport,
            Ycbcr444Support = ycbcr444Support,
            Ycbcr422Support = ycbcr422Support,
            VideoDescriptors = videoDescriptors,
            AudioDescriptors = audioDescriptors,
            HdmiVsdb = hdmiVsdb,
            HdmiForumVsdb = hdmiForumVsdb,
            HdrStaticMetadata = hdrStaticMetadata,
            SpeakerAllocation = speakerAllocation,
            Colorimetry = colorimetry,
            VideoCapability = videoCapability
        };
    }

    private static void ParseVideoDataBlock(byte[] data, List<VideoDescriptor> descriptors)
    {
        foreach (byte svd in data)
        {
            bool native = (svd & 0x80) != 0;
            int vic = svd & 0x7F;

            VideoMode? mode = null;
            if (VicTable.TryGetValue(vic, out var vicMode))
                mode = vicMode;

            descriptors.Add(new VideoDescriptor
            {
                Vic = vic,
                Native = native,
                Mode = mode
            });
        }
    }

    private static void ParseAudioDataBlock(byte[] data, List<AudioDescriptor> descriptors)
    {
        for (int i = 0; i < data.Length; i += 3)
        {
            if (i + 2 >= data.Length)
                break;

            byte b0 = data[i];
            byte b1 = data[i + 1];
            byte b2 = data[i + 2];

            int formatCode = (b0 >> 3) & 0x0F;
            int maxChannels = (b0 & 0x07) + 1;

            var sampleRates = new List<int>();
            if ((b1 & 0x01) != 0) sampleRates.Add(32);
            if ((b1 & 0x02) != 0) sampleRates.Add(44);
            if ((b1 & 0x04) != 0) sampleRates.Add(48);
            if ((b1 & 0x08) != 0) sampleRates.Add(88);
            if ((b1 & 0x10) != 0) sampleRates.Add(96);
            if ((b1 & 0x20) != 0) sampleRates.Add(176);
            if ((b1 & 0x40) != 0) sampleRates.Add(192);

            string format = formatCode switch
            {
                1 => "LPCM",
                2 => "AC-3",
                3 => "MPEG1",
                4 => "MP3",
                5 => "MPEG2",
                6 => "AAC-LC",
                7 => "DTS",
                8 => "ATRAC",
                9 => "DSD",
                10 => "E-AC-3",
                11 => "DTS-HD",
                12 => "MLP",
                13 => "DST",
                14 => "WMA Pro",
                15 => "Extended",
                _ => $"Reserved{formatCode}"
            };

            List<int>? bitDepths = null;
            int? maxBitrate = null;

            if (formatCode == 1) // LPCM
            {
                bitDepths = new List<int>();
                if ((b2 & 0x01) != 0) bitDepths.Add(16);
                if ((b2 & 0x02) != 0) bitDepths.Add(20);
                if ((b2 & 0x04) != 0) bitDepths.Add(24);
            }
            else if (formatCode >= 2 && formatCode <= 8)
            {
                maxBitrate = b2 * 8; // in kbps
            }

            descriptors.Add(new AudioDescriptor
            {
                Format = format,
                MaxChannels = maxChannels,
                SampleRatesKhz = sampleRates,
                BitDepths = bitDepths,
                MaxBitrateKbps = maxBitrate
            });
        }
    }

    private static void ParseVendorSpecificDataBlock(byte[] data, ref HdmiVsdb? hdmiVsdb, ref HdmiForumVsdb? hdmiForumVsdb)
    {
        if (data.Length < 3)
            return;

        uint oui = (uint)(data[0] | (data[1] << 8) | (data[2] << 16));

        if (oui == 0x000C03) // HDMI 1.4 OUI
        {
            hdmiVsdb = ParseHdmiVsdb(data.Skip(3).ToArray());
        }
        else if (oui == 0xC45DD8) // HDMI Forum OUI (HDMI 2.1)
        {
            hdmiForumVsdb = ParseHdmiForumVsdb(data.Skip(3).ToArray());
        }
    }

    private static HdmiVsdb? ParseHdmiVsdb(byte[] data)
    {
        if (data.Length < 2)
            return null;

        string? physicalAddress = null;
        if (data.Length >= 2)
        {
            physicalAddress = $"{(data[0] >> 4) & 0xF}.{data[0] & 0xF}.{(data[1] >> 4) & 0xF}.{data[1] & 0xF}";
        }

        bool supports30bit = false;
        bool supports36bit = false;
        bool supports48bit = false;
        bool supportsAi = false;
        int? maxTmdsClock = null;

        if (data.Length >= 3)
        {
            byte flags = data[2];
            supports30bit = (flags & 0x10) != 0;
            supports36bit = (flags & 0x20) != 0;
            supports48bit = (flags & 0x40) != 0;
            supportsAi = (flags & 0x80) != 0;
        }

        if (data.Length >= 4)
        {
            maxTmdsClock = data[3] * 5; // in MHz
        }

        return new HdmiVsdb
        {
            SourcePhysicalAddress = physicalAddress,
            Supports30bitDeepColor = supports30bit,
            Supports36bitDeepColor = supports36bit,
            Supports48bitDeepColor = supports48bit,
            SupportsAi = supportsAi,
            MaxTmdsClockMhz = maxTmdsClock
        };
    }

    private static HdmiForumVsdb? ParseHdmiForumVsdb(byte[] data)
    {
        if (data.Length < 1)
            return null;

        int version = data[0];
        int? maxFrlRate = null;
        bool supportsDsc = false;
        bool supportsAllm = false;
        bool supportsVrr = false;
        bool supportsQms = false;
        bool supportsQft = false;
        int? maxTmdsCharacterRate = null;

        if (data.Length >= 2)
        {
            maxTmdsCharacterRate = data[1] == 0 ? null : data[1] * 5;
        }

        if (data.Length >= 4)
        {
            maxFrlRate = data[3] & 0x0F;
            if (maxFrlRate == 0)
                maxFrlRate = null;
        }

        if (data.Length >= 5)
        {
            byte pb5 = data[4];
            supportsAllm = (pb5 & 0x40) != 0;
            supportsQms = (pb5 & 0x02) != 0;
            supportsQft = (pb5 & 0x08) != 0;
            supportsVrr = (pb5 & 0x10) != 0;
        }

        if (data.Length >= 7)
        {
            byte pb6 = data[5];
            byte pb7 = data[6];
            int vrrMin = pb6 & 0x3F;
            int vrrMax = ((pb6 >> 6) & 0x03) << 8 | pb7;
            supportsVrr = supportsVrr || (vrrMin > 0 && vrrMax > 0);
        }

        if (data.Length >= 8)
        {
            supportsDsc = (data[7] & 0x01) != 0;
        }

        return new HdmiForumVsdb
        {
            Version = version,
            MaxFrlRate = maxFrlRate,
            SupportsDsc = supportsDsc,
            SupportsAllm = supportsAllm,
            SupportsVrr = supportsVrr,
            SupportsQms = supportsQms,
            SupportsQft = supportsQft,
            MaxTmdsCharacterRateMhz = maxTmdsCharacterRate
        };
    }

    private static HdrStaticMetadata? ParseHdrStaticMetadata(byte[] data)
    {
        if (data.Length < 1)
            return null;

        var eotfs = new List<string>();
        if (data.Length >= 1)
        {
            byte eotfFlags = data[0];
            if ((eotfFlags & 0x01) != 0) eotfs.Add("SDR");
            if ((eotfFlags & 0x02) != 0) eotfs.Add("HDR10");
            if ((eotfFlags & 0x04) != 0) eotfs.Add("HLG");
        }

        var metadataTypes = new List<int>();
        if (data.Length >= 2)
        {
            byte typeFlags = data[1];
            for (int i = 0; i < 8; i++)
            {
                if ((typeFlags & (1 << i)) != 0)
                    metadataTypes.Add(i);
            }
        }

        int? maxLuminance = null;
        int? maxFrameAvgLuminance = null;
        double? minLuminance = null;

        if (data.Length >= 3 && data[2] != 0)
        {
            maxLuminance = 50 * (int)Math.Pow(2, data[2] / 32.0);
        }

        if (data.Length >= 4 && data[3] != 0)
        {
            maxFrameAvgLuminance = 50 * (int)Math.Pow(2, data[3] / 32.0);
        }

        if (data.Length >= 5 && data[4] != 0)
        {
            minLuminance = (maxLuminance ?? 100) * Math.Pow(data[4] / 255.0, 2) / 100.0;
        }

        return new HdrStaticMetadata
        {
            Eotfs = eotfs,
            StaticMetadataTypes = metadataTypes,
            MaxLuminanceCdM2 = maxLuminance,
            MaxFrameAvgLuminanceCdM2 = maxFrameAvgLuminance,
            MinLuminanceCdM2 = minLuminance
        };
    }

    private static SpeakerAllocation? ParseSpeakerAllocation(byte[] data)
    {
        if (data.Length < 1)
            return null;

        byte flags = data[0];
        return new SpeakerAllocation
        {
            FrontLeftRight = (flags & 0x01) != 0,
            Lfe = (flags & 0x02) != 0,
            FrontCenter = (flags & 0x04) != 0,
            RearLeftRight = (flags & 0x08) != 0,
            RearCenter = (flags & 0x10) != 0,
            FrontCenterLeftRight = (flags & 0x20) != 0,
            RearCenterLeftRight = (flags & 0x40) != 0
        };
    }

    private static ColorimetryDataBlock? ParseColorimetryDataBlock(byte[] data)
    {
        if (data.Length < 1)
            return null;

        byte flags = data[0];
        byte flags2 = data.Length >= 2 ? data[1] : (byte)0;

        return new ColorimetryDataBlock
        {
            Xvycc601 = (flags & 0x01) != 0,
            Xvycc709 = (flags & 0x02) != 0,
            Sycc601 = (flags & 0x04) != 0,
            AdobeYcc601 = (flags & 0x08) != 0,
            AdobeRgb = (flags & 0x10) != 0,
            Bt2020Ycc = (flags & 0x20) != 0,
            Bt2020Rgb = (flags & 0x40) != 0,
            DciP3 = (flags2 & 0x80) != 0
        };
    }

    private static VideoCapabilityDataBlock? ParseVideoCapabilityDataBlock(byte[] data)
    {
        if (data.Length < 1)
            return null;

        byte flags = data[0];
        bool qsSelectable = (flags & 0x40) != 0;

        string? preferredTimingMode = null;
        int ptMode = (flags >> 4) & 0x03;
        if (ptMode == 0) preferredTimingMode = "OverscannedTV";
        else if (ptMode == 1) preferredTimingMode = "UnderscannedTV";
        else if (ptMode == 2) preferredTimingMode = "Both";

        return new VideoCapabilityDataBlock
        {
            QuantizationRangeSelectable = qsSelectable,
            PreferredTimingMode = preferredTimingMode
        };
    }

    private static Dictionary<int, VideoMode> InitializeVicTable()
    {
        return new Dictionary<int, VideoMode>
        {
            [1] = new VideoMode { WidthPx = 640, HeightPx = 480, RefreshRateHz = 60 },
            [2] = new VideoMode { WidthPx = 720, HeightPx = 480, RefreshRateHz = 60 },
            [3] = new VideoMode { WidthPx = 720, HeightPx = 480, RefreshRateHz = 60 },
            [4] = new VideoMode { WidthPx = 1280, HeightPx = 720, RefreshRateHz = 60 },
            [5] = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 60, Interlaced = true },
            [16] = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 60 },
            [17] = new VideoMode { WidthPx = 720, HeightPx = 576, RefreshRateHz = 50 },
            [18] = new VideoMode { WidthPx = 720, HeightPx = 576, RefreshRateHz = 50 },
            [19] = new VideoMode { WidthPx = 1280, HeightPx = 720, RefreshRateHz = 50 },
            [20] = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 50, Interlaced = true },
            [31] = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 50 },
            [32] = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 24 },
            [33] = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 25 },
            [34] = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 30 },
            [63] = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 120 },
            [93] = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 24 },
            [94] = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 25 },
            [95] = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 30 },
            [96] = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 50 },
            [97] = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 60 },
            [117] = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 100 },
            [118] = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 120 },
            [193] = new VideoMode { WidthPx = 5120, HeightPx = 2160, RefreshRateHz = 120 },
            [194] = new VideoMode { WidthPx = 7680, HeightPx = 4320, RefreshRateHz = 24 },
            [195] = new VideoMode { WidthPx = 7680, HeightPx = 4320, RefreshRateHz = 25 },
            [196] = new VideoMode { WidthPx = 7680, HeightPx = 4320, RefreshRateHz = 30 },
            [197] = new VideoMode { WidthPx = 7680, HeightPx = 4320, RefreshRateHz = 48 },
            [198] = new VideoMode { WidthPx = 7680, HeightPx = 4320, RefreshRateHz = 50 },
            [199] = new VideoMode { WidthPx = 7680, HeightPx = 4320, RefreshRateHz = 60 },
            [218] = new VideoMode { WidthPx = 7680, HeightPx = 4320, RefreshRateHz = 100 },
            [219] = new VideoMode { WidthPx = 7680, HeightPx = 4320, RefreshRateHz = 120 }
        };
    }
}
