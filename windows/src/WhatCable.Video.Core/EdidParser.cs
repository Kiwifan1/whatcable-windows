using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhatCable.Video.Core;

/// <summary>
/// EDID 1.4 parser for display information.
/// </summary>
public static class EdidParser
{
    private static readonly byte[] EdidHeader = { 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00 };

    /// <summary>
    /// Parse EDID data and return structured information.
    /// </summary>
    public static EdidInfo? Parse(byte[] edid)
    {
        if (edid == null || edid.Length < 128)
            return null;

        // Verify header
        if (!VerifyHeader(edid))
            return null;

        // Verify checksum
        if (!VerifyChecksum(edid.Take(128).ToArray()))
            return null;

        var manufacturer = ParseManufacturerId(edid);
        var productCode = ReadUInt16LittleEndian(edid, 10);
        var serialNumber = ReadUInt32LittleEndian(edid, 12);
        var manufactureWeek = edid[16];
        var manufactureYear = edid[17] > 0 ? 1990 + edid[17] : (int?)null;
        var version = $"{edid[18]}.{edid[19]}";

        // Parse basic display parameters
        var widthCm = edid[21] > 0 ? (int?)edid[21] : null;
        var heightCm = edid[22] > 0 ? (int?)edid[22] : null;

        // Parse established timings, standard timings, and detailed timings
        var supportedModes = new List<VideoMode>();
        ParseEstablishedTimings(edid, supportedModes);
        ParseStandardTimings(edid, supportedModes);

        // Parse 18-byte descriptors (detailed timings and monitor descriptors)
        string? displayName = null;
        VideoMode? nativeResolution = null;
        double? maxPixelClockMhz = null;

        for (int i = 0; i < 4; i++)
        {
            int offset = 54 + (i * 18);
            if (offset + 18 > edid.Length)
                break;

            // Check if this is a detailed timing descriptor (pixel clock != 0)
            ushort pixelClock = ReadUInt16LittleEndian(edid, offset);
            if (pixelClock != 0)
            {
                var mode = ParseDetailedTiming(edid, offset);
                if (mode != null)
                {
                    supportedModes.Add(mode);
                    if (nativeResolution == null)
                        nativeResolution = mode;
                    if (mode.PixelClockMhz.HasValue)
                    {
                        if (!maxPixelClockMhz.HasValue || mode.PixelClockMhz.Value > maxPixelClockMhz.Value)
                            maxPixelClockMhz = mode.PixelClockMhz.Value;
                    }
                }
            }
            else
            {
                // Monitor descriptor
                byte descriptorType = edid[offset + 3];
                if (descriptorType == 0xFC) // Display name
                {
                    displayName = ParseDisplayName(edid, offset + 5);
                }
            }
        }

        var extensionBlocks = edid.Length >= 127 ? edid[126] : 0;

        // Parse CTA extension if present
        CtaInfo? ctaInfo = null;
        DisplayIdInfo? displayIdInfo = null;

        if (extensionBlocks > 0 && edid.Length >= 256)
        {
            byte extensionTag = edid[128];
            if (extensionTag == 0x02) // CEA/CTA-861
            {
                ctaInfo = CtaParser.Parse(edid.Skip(128).Take(128).ToArray());
            }
            else if (extensionTag == 0x70) // DisplayID
            {
                displayIdInfo = DisplayIdParser.Parse(edid.Skip(128).ToArray());
            }
        }

        // Check for additional extension blocks
        for (int i = 1; i < extensionBlocks && (128 * (i + 1)) < edid.Length; i++)
        {
            int blockOffset = 128 * (i + 1);
            byte extensionTag = edid[blockOffset];

            if (extensionTag == 0x02 && ctaInfo == null)
            {
                ctaInfo = CtaParser.Parse(edid.Skip(blockOffset).Take(128).ToArray());
            }
            else if (extensionTag == 0x70 && displayIdInfo == null)
            {
                displayIdInfo = DisplayIdParser.Parse(edid.Skip(blockOffset).ToArray());
            }
        }

        return new EdidInfo
        {
            Version = version,
            Manufacturer = manufacturer,
            ProductCode = productCode,
            SerialNumber = serialNumber,
            ManufactureWeek = manufactureWeek > 0 ? (int?)manufactureWeek : null,
            ManufactureYear = manufactureYear,
            DisplayName = displayName,
            WidthCm = widthCm,
            HeightCm = heightCm,
            NativeResolution = nativeResolution,
            MaxPixelClockMhz = maxPixelClockMhz,
            SupportedModes = supportedModes,
            ExtensionBlocks = extensionBlocks,
            CtaInfo = ctaInfo,
            DisplayIdInfo = displayIdInfo
        };
    }

    private static bool VerifyHeader(byte[] edid)
    {
        for (int i = 0; i < EdidHeader.Length; i++)
        {
            if (edid[i] != EdidHeader[i])
                return false;
        }
        return true;
    }

    private static bool VerifyChecksum(byte[] block)
    {
        byte sum = 0;
        for (int i = 0; i < 128; i++)
        {
            sum += block[i];
        }
        return sum == 0;
    }

    private static string? ParseManufacturerId(byte[] edid)
    {
        ushort id = (ushort)((edid[8] << 8) | edid[9]);

        char? c1 = DecodeManufacturerCharacter((id >> 10) & 0x1F);
        char? c2 = DecodeManufacturerCharacter((id >> 5) & 0x1F);
        char? c3 = DecodeManufacturerCharacter(id & 0x1F);

        if (!c1.HasValue || !c2.HasValue || !c3.HasValue)
            return null;

        return $"{c1}{c2}{c3}";
    }

    private static char? DecodeManufacturerCharacter(int value)
    {
        if (value is < 1 or > 26)
            return null;

        return (char)('A' + value - 1);
    }

    private static ushort ReadUInt16LittleEndian(byte[] data, int offset)
        => (ushort)(data[offset] | (data[offset + 1] << 8));

    private static uint ReadUInt32LittleEndian(byte[] data, int offset)
        => (uint)(data[offset]
                  | (data[offset + 1] << 8)
                  | (data[offset + 2] << 16)
                  | (data[offset + 3] << 24));

    private static string ParseDisplayName(byte[] edid, int offset)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 13; i++)
        {
            byte b = edid[offset + i];
            if (b == 0x0A || b == 0x00)
                break;
            sb.Append((char)b);
        }
        return sb.ToString().Trim();
    }

    private static void ParseEstablishedTimings(byte[] edid, List<VideoMode> modes)
    {
        byte et1 = edid[35];
        byte et2 = edid[36];
        byte etm = edid[37];

        // Established Timings I (byte 35)
        if ((et1 & 0x80) != 0) modes.Add(new VideoMode { WidthPx = 720, HeightPx = 400, RefreshRateHz = 70 });
        if ((et1 & 0x40) != 0) modes.Add(new VideoMode { WidthPx = 720, HeightPx = 400, RefreshRateHz = 88 });
        if ((et1 & 0x20) != 0) modes.Add(new VideoMode { WidthPx = 640, HeightPx = 480, RefreshRateHz = 60 });
        if ((et1 & 0x10) != 0) modes.Add(new VideoMode { WidthPx = 640, HeightPx = 480, RefreshRateHz = 67 });
        if ((et1 & 0x08) != 0) modes.Add(new VideoMode { WidthPx = 640, HeightPx = 480, RefreshRateHz = 72 });
        if ((et1 & 0x04) != 0) modes.Add(new VideoMode { WidthPx = 640, HeightPx = 480, RefreshRateHz = 75 });
        if ((et1 & 0x02) != 0) modes.Add(new VideoMode { WidthPx = 800, HeightPx = 600, RefreshRateHz = 56 });
        if ((et1 & 0x01) != 0) modes.Add(new VideoMode { WidthPx = 800, HeightPx = 600, RefreshRateHz = 60 });

        // Established Timings II (byte 36)
        if ((et2 & 0x80) != 0) modes.Add(new VideoMode { WidthPx = 800, HeightPx = 600, RefreshRateHz = 72 });
        if ((et2 & 0x40) != 0) modes.Add(new VideoMode { WidthPx = 800, HeightPx = 600, RefreshRateHz = 75 });
        if ((et2 & 0x20) != 0) modes.Add(new VideoMode { WidthPx = 832, HeightPx = 624, RefreshRateHz = 75 });
        if ((et2 & 0x10) != 0) modes.Add(new VideoMode { WidthPx = 1024, HeightPx = 768, RefreshRateHz = 87, Interlaced = true });
        if ((et2 & 0x08) != 0) modes.Add(new VideoMode { WidthPx = 1024, HeightPx = 768, RefreshRateHz = 60 });
        if ((et2 & 0x04) != 0) modes.Add(new VideoMode { WidthPx = 1024, HeightPx = 768, RefreshRateHz = 70 });
        if ((et2 & 0x02) != 0) modes.Add(new VideoMode { WidthPx = 1024, HeightPx = 768, RefreshRateHz = 75 });
        if ((et2 & 0x01) != 0) modes.Add(new VideoMode { WidthPx = 1280, HeightPx = 1024, RefreshRateHz = 75 });
    }

    private static void ParseStandardTimings(byte[] edid, List<VideoMode> modes)
    {
        for (int i = 0; i < 8; i++)
        {
            int offset = 38 + (i * 2);
            byte b1 = edid[offset];
            byte b2 = edid[offset + 1];

            if (b1 == 0x01 && b2 == 0x01)
                continue; // Unused

            int hres = (b1 + 31) * 8;
            int aspectRatio = (b2 >> 6) & 0x03;
            int vres = aspectRatio switch
            {
                0 => (hres * 10) / 16,  // 16:10
                1 => (hres * 3) / 4,    // 4:3
                2 => (hres * 4) / 5,    // 5:4
                3 => (hres * 9) / 16,   // 16:9
                _ => hres
            };
            int refreshRate = (b2 & 0x3F) + 60;

            modes.Add(new VideoMode { WidthPx = hres, HeightPx = vres, RefreshRateHz = refreshRate });
        }
    }

    private static VideoMode? ParseDetailedTiming(byte[] edid, int offset)
    {
        try
        {
            ushort pixelClock10kHz = ReadUInt16LittleEndian(edid, offset);
            if (pixelClock10kHz == 0)
                return null;

            double pixelClockMhz = pixelClock10kHz / 100.0;

            byte hActiveLsb = edid[offset + 2];
            byte hBlankingLsb = edid[offset + 3];
            byte hActiveBlankingMsb = edid[offset + 4];

            int hActive = hActiveLsb | ((hActiveBlankingMsb & 0xF0) << 4);
            int hBlanking = hBlankingLsb | ((hActiveBlankingMsb & 0x0F) << 8);

            byte vActiveLsb = edid[offset + 5];
            byte vBlankingLsb = edid[offset + 6];
            byte vActiveBlankingMsb = edid[offset + 7];

            int vActive = vActiveLsb | ((vActiveBlankingMsb & 0xF0) << 4);
            int vBlanking = vBlankingLsb | ((vActiveBlankingMsb & 0x0F) << 8);

            byte flags = edid[offset + 17];
            bool interlaced = (flags & 0x80) != 0;

            double totalPixels = (hActive + hBlanking) * (vActive + vBlanking);
            double refreshRate = (pixelClockMhz * 1_000_000) / totalPixels;

            if (interlaced)
                refreshRate *= 2;

            return new VideoMode
            {
                WidthPx = hActive,
                HeightPx = vActive,
                RefreshRateHz = Math.Round(refreshRate, 2),
                Interlaced = interlaced,
                PixelClockMhz = pixelClockMhz
            };
        }
        catch
        {
            return null;
        }
    }
}
