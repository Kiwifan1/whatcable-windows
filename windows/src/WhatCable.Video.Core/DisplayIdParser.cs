using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatCable.Video.Core;

/// <summary>
/// DisplayID 2.0 parser for DisplayPort UHBR capabilities.
/// </summary>
public static class DisplayIdParser
{
    /// <summary>
    /// Parse DisplayID 2.0 extension block.
    /// </summary>
    public static DisplayIdInfo? Parse(byte[] displayIdBlock)
    {
        if (displayIdBlock == null || displayIdBlock.Length < 128)
            return null;

        byte tag = displayIdBlock[0];
        if (tag != 0x70) // DisplayID extension tag
            return null;

        // DisplayID structure starts at offset 1
        byte structVersion = displayIdBlock[1];
        byte revision = (byte)((structVersion >> 4) & 0x0F);
        byte version = (byte)(structVersion & 0x0F);

        if (version < 2) // We're looking for DisplayID 2.0+
            return null;

        byte sectionBytes = displayIdBlock[2];
        byte productType = displayIdBlock[3];

        string? primaryUseCase = productType switch
        {
            0x00 => "Extension",
            0x01 => "Test",
            0x02 => "Generic Display",
            0x03 => "Television",
            0x04 => "Desktop Productivity",
            0x05 => "Desktop Gaming",
            0x06 => "Presentation",
            0x07 => "Head Mounted VR",
            0x08 => "Head Mounted AR",
            _ => $"Unknown ({productType})"
        };

        var supportedModes = new List<VideoMode>();
        var uhbrSupport = new List<string>();
        string? maxLinkRate = null;

        // Parse data blocks
        int offset = 5;
        while (offset < sectionBytes + 5 && offset < displayIdBlock.Length - 3)
        {
            byte blockType = displayIdBlock[offset];
            byte blockRevision = displayIdBlock[offset + 1];
            byte blockLength = displayIdBlock[offset + 2];

            if (offset + blockLength + 3 > displayIdBlock.Length)
                break;

            byte[] blockData = displayIdBlock.Skip(offset + 3).Take(blockLength).ToArray();

            switch (blockType)
            {
                case 0x03: // Type III Timing - Detailed Timings
                    ParseDetailedTimings(blockData, supportedModes);
                    break;
                case 0x09: // DisplayID Type IX Timing - Formula-based Timings (DisplayID 2.0)
                    ParseFormulaBasedTimings(blockData, supportedModes);
                    break;
                case 0x0A: // Dynamic Video Timing Range Limits (DisplayID 2.0)
                    // Could extract max refresh rate info here
                    break;
                case 0x29: // DisplayPort capabilities (vendor-specific)
                    ParseDisplayPortCapabilities(blockData, uhbrSupport, ref maxLinkRate);
                    break;
            }

            offset += blockLength + 3;
        }

        return new DisplayIdInfo
        {
            Version = $"{version}.{revision}",
            PrimaryUseCase = primaryUseCase,
            MaxLinkRate = maxLinkRate,
            UhbrSupport = uhbrSupport,
            SupportedModes = supportedModes
        };
    }

    private static void ParseDetailedTimings(byte[] data, List<VideoMode> modes)
    {
        // Type III detailed timing descriptors are 20 bytes each
        for (int i = 0; i + 20 <= data.Length; i += 20)
        {
            try
            {
                uint pixelClock = BitConverter.ToUInt32(data, i) & 0x00FFFFFF; // 24-bit pixel clock in kHz
                if (pixelClock == 0)
                    continue;

                double pixelClockMhz = pixelClock / 1000.0;

                ushort hActive = (ushort)(BitConverter.ToUInt16(data, i + 3) + 1);
                ushort hBlanking = (ushort)(BitConverter.ToUInt16(data, i + 5) + 1);
                ushort vActive = (ushort)(BitConverter.ToUInt16(data, i + 7) + 1);
                ushort vBlanking = (ushort)(BitConverter.ToUInt16(data, i + 9) + 1);

                byte options = data[i + 17];
                bool interlaced = (options & 0x10) != 0;

                double totalPixels = (hActive + hBlanking) * (vActive + vBlanking);
                double refreshRate = (pixelClockMhz * 1_000_000) / totalPixels;

                if (interlaced)
                    refreshRate *= 2;

                modes.Add(new VideoMode
                {
                    WidthPx = hActive,
                    HeightPx = vActive,
                    RefreshRateHz = Math.Round(refreshRate, 2),
                    Interlaced = interlaced,
                    PixelClockMhz = pixelClockMhz
                });
            }
            catch
            {
                // Skip malformed timing descriptor
            }
        }
    }

    private static void ParseFormulaBasedTimings(byte[] data, List<VideoMode> modes)
    {
        // Type IX timing uses formula-based timing generation
        // This is a simplified parser that extracts basic resolution info
        for (int i = 0; i + 11 <= data.Length; i += 11)
        {
            try
            {
                uint pixelClock = BitConverter.ToUInt32(data, i) & 0x00FFFFFF; // 24-bit pixel clock in kHz
                if (pixelClock == 0)
                    continue;

                ushort hActive = (ushort)(BitConverter.ToUInt16(data, i + 3) + 1);
                ushort vActive = (ushort)(BitConverter.ToUInt16(data, i + 7) + 1);

                byte options = data[i + 10];
                bool interlaced = (options & 0x10) != 0;

                // Simplified refresh rate estimation
                double pixelClockMhz = pixelClock / 1000.0;
                double totalH = hActive * 1.25; // Rough estimate with blanking
                double totalV = vActive * 1.05; // Rough estimate with blanking
                double refreshRate = (pixelClockMhz * 1_000_000) / (totalH * totalV);

                if (interlaced)
                    refreshRate *= 2;

                modes.Add(new VideoMode
                {
                    WidthPx = hActive,
                    HeightPx = vActive,
                    RefreshRateHz = Math.Round(refreshRate, 2),
                    Interlaced = interlaced,
                    PixelClockMhz = pixelClockMhz
                });
            }
            catch
            {
                // Skip malformed timing descriptor
            }
        }
    }

    private static void ParseDisplayPortCapabilities(byte[] data, List<string> uhbrSupport, ref string? maxLinkRate)
    {
        if (data.Length < 2)
            return;

        // This is a vendor-specific block, so the format may vary
        // We'll look for common DisplayPort capability indicators

        // Check for UHBR support indicators (DisplayPort 2.0)
        // These are typical bit patterns found in DP capability blocks
        if (data.Length >= 4)
        {
            byte dpCapabilities = data[0];
            byte uhbrCapabilities = data.Length > 3 ? data[3] : (byte)0;

            // Check for standard DP rates
            if ((dpCapabilities & 0x01) != 0)
            {
                maxLinkRate = "HBR (2.7 Gbps)";
            }
            if ((dpCapabilities & 0x02) != 0)
            {
                maxLinkRate = "HBR2 (5.4 Gbps)";
            }
            if ((dpCapabilities & 0x04) != 0)
            {
                maxLinkRate = "HBR3 (8.1 Gbps)";
            }

            // Check for UHBR rates (DP 2.0)
            if ((uhbrCapabilities & 0x01) != 0)
            {
                uhbrSupport.Add("UHBR10");
                maxLinkRate = "UHBR10 (10 Gbps)";
            }
            if ((uhbrCapabilities & 0x02) != 0)
            {
                uhbrSupport.Add("UHBR13.5");
                maxLinkRate = "UHBR13.5 (13.5 Gbps)";
            }
            if ((uhbrCapabilities & 0x04) != 0)
            {
                uhbrSupport.Add("UHBR20");
                maxLinkRate = "UHBR20 (20 Gbps)";
            }
        }
    }
}
