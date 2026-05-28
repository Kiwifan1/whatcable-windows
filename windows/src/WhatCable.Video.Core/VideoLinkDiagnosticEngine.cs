using System;
using System.Collections.Generic;
using System.Text;

namespace WhatCable.Video.Core;

/// <summary>
/// Video link diagnostic engine - determines what's limiting the video link.
/// </summary>
public static class VideoLinkDiagnostic
{
    /// <summary>
    /// Analyze a video port snapshot and determine the bottleneck.
    /// </summary>
    public static VideoLinkDiagnosticResult Analyze(VideoPortSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return new VideoLinkDiagnosticResult
            {
                Bottleneck = VideoBottleneck.Unknown,
                Verdict = "No video port information available."
            };
        }

        var details = new List<string>();
        VideoBottleneck bottleneck = VideoBottleneck.Unknown;
        VideoCableClass? recommendedCableClass = null;

        // Extract capabilities from snapshot
        var activeMode = snapshot.ActiveMode;
        var sinkMaxMode = snapshot.SinkMaxMode;
        var cableClass = snapshot.AdvertisedCableClass;
        var gpuCaps = snapshot.SourceGpuCaps;
        var edid = snapshot.EdidParsed;

        // If no active mode, check if there's a connection issue
        if (activeMode == null)
        {
            if (sinkMaxMode != null)
            {
                bottleneck = VideoBottleneck.Cable;
                details.Add("No active video signal detected.");
                details.Add($"Display supports up to {FormatMode(sinkMaxMode)}.");
                recommendedCableClass = RecommendCableForMode(sinkMaxMode, snapshot.ConnectorType);
                return BuildDiagnostic(bottleneck, details, recommendedCableClass);
            }
            else
            {
                bottleneck = VideoBottleneck.Unknown;
                details.Add("No display connected or EDID not readable.");
                return BuildDiagnostic(bottleneck, details, recommendedCableClass);
            }
        }

        // Add active mode info
        details.Add($"Active mode: {FormatMode(activeMode)}");

        // Determine display capabilities
        if (sinkMaxMode != null)
        {
            details.Add($"Display maximum: {FormatMode(sinkMaxMode)}");
        }

        // Calculate required bandwidth for active and max modes
        double activeBandwidth = CalculateRequiredBandwidth(activeMode);
        double? maxSinkBandwidth = sinkMaxMode != null ? CalculateRequiredBandwidth(sinkMaxMode) : null;

        // Check if active mode matches sink capability
        bool runningAtMaxSinkCapability = false;
        if (sinkMaxMode != null)
        {
            runningAtMaxSinkCapability = ModesAreEquivalent(activeMode, sinkMaxMode);
        }

        // Determine cable capability
        double? cableBandwidth = GetCableBandwidth(cableClass, snapshot.ConnectorType);
        if (cableClass.HasValue)
        {
            details.Add($"Cable class: {cableClass.Value}");
        }

        // Determine GPU capability
        double? gpuBandwidth = GetGpuBandwidth(gpuCaps, snapshot.ConnectorType);
        if (gpuCaps != null && !string.IsNullOrEmpty(gpuCaps.Model))
        {
            details.Add($"GPU: {gpuCaps.Vendor} {gpuCaps.Model}");
        }

        // Analyze bottleneck
        if (runningAtMaxSinkCapability)
        {
            // Running at display's max capability - this is optimal
            bottleneck = VideoBottleneck.None;
            details.Add("Running at display's maximum capability. Optimal configuration.");
        }
        else if (sinkMaxMode != null && maxSinkBandwidth.HasValue)
        {
            // Not running at max sink capability - determine why
            bool cableLimiting = cableBandwidth.HasValue && maxSinkBandwidth.Value > cableBandwidth.Value * 0.9;
            bool gpuLimiting = gpuBandwidth.HasValue && maxSinkBandwidth.Value > gpuBandwidth.Value * 0.9;

            if (cableLimiting && !gpuLimiting)
            {
                bottleneck = VideoBottleneck.Cable;
                details.Add($"Cable limits to {FormatMode(activeMode)}. Display supports {FormatMode(sinkMaxMode)}.");
                recommendedCableClass = RecommendCableForMode(sinkMaxMode, snapshot.ConnectorType);
            }
            else if (gpuLimiting && !cableLimiting)
            {
                bottleneck = VideoBottleneck.Source;
                details.Add($"GPU limits to {FormatMode(activeMode)}. Display supports {FormatMode(sinkMaxMode)}.");
            }
            else if (cableLimiting && gpuLimiting)
            {
                // Both limiting - choose the more restrictive
                if (cableBandwidth.HasValue && gpuBandwidth.HasValue)
                {
                    if (cableBandwidth.Value < gpuBandwidth.Value)
                    {
                        bottleneck = VideoBottleneck.Cable;
                        details.Add($"Cable is primary bottleneck. GPU also limiting.");
                        recommendedCableClass = RecommendCableForMode(sinkMaxMode, snapshot.ConnectorType);
                    }
                    else
                    {
                        bottleneck = VideoBottleneck.Source;
                        details.Add($"GPU is primary bottleneck. Cable also limiting.");
                        recommendedCableClass = RecommendCableForMode(sinkMaxMode, snapshot.ConnectorType);
                    }
                }
                else
                {
                    bottleneck = VideoBottleneck.Unknown;
                    details.Add($"Not running at display maximum. Bottleneck unclear.");
                }
            }
            else
            {
                // Display supports more but we don't know why it's not being used
                bottleneck = VideoBottleneck.Sink;
                details.Add($"Display reports {FormatMode(sinkMaxMode)} capability but not active.");
                details.Add("This may be a user setting or OS limitation.");
            }
        }
        else
        {
            // No sink max mode info
            bottleneck = VideoBottleneck.Unknown;
            details.Add("Insufficient information to determine bottleneck.");
        }

        return BuildDiagnostic(bottleneck, details, recommendedCableClass);
    }

    private static VideoLinkDiagnosticResult BuildDiagnostic(VideoBottleneck bottleneck, List<string> details, VideoCableClass? recommendedCableClass)
    {
        string verdict = bottleneck switch
        {
            VideoBottleneck.None => "✓ Optimal configuration - running at display's maximum capability.",
            VideoBottleneck.Cable => "⚠ Cable is limiting the video link. Consider upgrading the cable.",
            VideoBottleneck.Source => "⚠ GPU/source is limiting the video link. Hardware upgrade needed for higher modes.",
            VideoBottleneck.Sink => "ℹ Display is the limiting factor or mode selection is intentional.",
            _ => "? Unable to determine what's limiting the video link."
        };

        return new VideoLinkDiagnosticResult
        {
            Bottleneck = bottleneck,
            Verdict = verdict,
            Details = details,
            RecommendedCableClass = recommendedCableClass
        };
    }

    private static string FormatMode(VideoMode mode)
    {
        string resolution = $"{mode.WidthPx}×{mode.HeightPx}";
        string refresh = $"{mode.RefreshRateHz:F0}Hz";
        string interlaced = mode.Interlaced ? "i" : "";
        return $"{resolution}@{refresh}{interlaced}";
    }

    private static bool ModesAreEquivalent(VideoMode a, VideoMode b)
    {
        return a.WidthPx == b.WidthPx &&
               a.HeightPx == b.HeightPx &&
               Math.Abs(a.RefreshRateHz - b.RefreshRateHz) < 1.0 &&
               a.Interlaced == b.Interlaced;
    }

    private static double CalculateRequiredBandwidth(VideoMode mode)
    {
        if (mode.PixelClockMhz.HasValue)
        {
            // Use actual pixel clock if available
            return mode.PixelClockMhz.Value;
        }

        // Estimate bandwidth based on resolution and refresh rate
        // Assume ~25% overhead for blanking intervals
        double pixelsPerSecond = mode.WidthPx * mode.HeightPx * mode.RefreshRateHz;
        double pixelClockMhz = (pixelsPerSecond * 1.25) / 1_000_000;

        // For HDMI, multiply by bits per pixel (24-bit color = 3 bytes)
        // This gives us approximate TMDS clock rate
        return pixelClockMhz;
    }

    private static double? GetCableBandwidth(VideoCableClass? cableClass, VideoConnectorType connectorType)
    {
        if (!cableClass.HasValue)
            return null;

        return cableClass.Value switch
        {
            VideoCableClass.HdmiStandard => 165.0,        // HDMI 1.4: 165 MHz TMDS (up to 4K30)
            VideoCableClass.HdmiHighSpeed => 600.0,       // HDMI 2.0: 600 MHz TMDS (up to 4K60)
            VideoCableClass.HdmiUltraHighSpeed => 1200.0, // HDMI 2.1: 48 Gbps FRL
            VideoCableClass.DisplayPort14 => 810.0,       // DP 1.4 HBR3: 8.1 Gbps per lane × 4 = 32.4 Gbps
            VideoCableClass.DisplayPort20 => 1000.0,      // DP 2.0 UHBR10: 10 Gbps per lane × 4 = 40 Gbps
            VideoCableClass.DisplayPort20Uhbr135 => 1350.0, // DP 2.0 UHBR13.5: 54 Gbps
            VideoCableClass.DisplayPort20Uhbr20 => 2000.0,  // DP 2.0 UHBR20: 80 Gbps
            VideoCableClass.UsbC31Gen1 => 540.0,          // USB-C with DP Alt Mode HBR2: 5.4 Gbps per lane
            VideoCableClass.UsbC31Gen2 => 810.0,          // USB-C with DP Alt Mode HBR3: 8.1 Gbps per lane
            VideoCableClass.UsbC4Gen3 => 2000.0,          // USB4 with DP 2.0: up to 80 Gbps
            _ => null
        };
    }

    private static double? GetGpuBandwidth(GpuCapabilities? gpuCaps, VideoConnectorType connectorType)
    {
        if (gpuCaps == null)
            return null;

        // Use max pixel clock if available
        if (gpuCaps.MaxPixelClockMhz.HasValue)
            return gpuCaps.MaxPixelClockMhz.Value;

        // Otherwise estimate from HDMI/DP generation
        if (connectorType == VideoConnectorType.HDMI && !string.IsNullOrEmpty(gpuCaps.MaxHdmiGen))
        {
            return gpuCaps.MaxHdmiGen switch
            {
                "1.4" => 165.0,
                "2.0" => 600.0,
                "2.1" => 1200.0,
                _ => null
            };
        }

        if ((connectorType == VideoConnectorType.DisplayPort || connectorType == VideoConnectorType.UsbCDisplayPort) &&
            !string.IsNullOrEmpty(gpuCaps.MaxDisplayPortGen))
        {
            return gpuCaps.MaxDisplayPortGen switch
            {
                "1.2" => 540.0,
                "1.4" => 810.0,
                "2.0" => 2000.0,
                _ => null
            };
        }

        return null;
    }

    private static VideoCableClass? RecommendCableForMode(VideoMode mode, VideoConnectorType connectorType)
    {
        return connectorType switch
        {
            VideoConnectorType.DisplayPort => RecommendDisplayPortCableForMode(mode),
            VideoConnectorType.UsbCDisplayPort => RecommendUsbCCableForMode(mode),
            VideoConnectorType.Thunderbolt => RecommendUsbCCableForMode(mode),
            _ => RecommendHdmiCableForMode(mode)
        };
    }

    private static VideoCableClass RecommendHdmiCableForMode(VideoMode mode)
    {
        bool is4K = mode.HeightPx >= 2160;
        bool is8K = mode.HeightPx >= 4320;
        bool highRefresh = mode.RefreshRateHz >= 120;

        if (is8K || (is4K && highRefresh))
        {
            // Need HDMI 2.1 or DP 2.0
            return VideoCableClass.HdmiUltraHighSpeed;
        }
        else if (is4K && mode.RefreshRateHz >= 60)
        {
            // Need HDMI 2.0 or DP 1.4
            return VideoCableClass.HdmiHighSpeed;
        }
        else if (is4K)
        {
            // 4K30 - HDMI 1.4 sufficient
            return VideoCableClass.HdmiStandard;
        }
        else
        {
            // 1080p or lower
            return VideoCableClass.HdmiStandard;
        }
    }

    private static VideoCableClass RecommendDisplayPortCableForMode(VideoMode mode)
    {
        bool is4K = mode.HeightPx >= 2160;
        bool is8K = mode.HeightPx >= 4320;
        bool highRefresh = mode.RefreshRateHz >= 120;

        if (is8K || (is4K && highRefresh))
        {
            return VideoCableClass.DisplayPort20Uhbr20;
        }

        return VideoCableClass.DisplayPort14;
    }

    private static VideoCableClass RecommendUsbCCableForMode(VideoMode mode)
    {
        bool is4K = mode.HeightPx >= 2160;
        bool is8K = mode.HeightPx >= 4320;
        bool highRefresh = mode.RefreshRateHz >= 120;

        if (is8K || (is4K && highRefresh))
        {
            return VideoCableClass.UsbC4Gen3;
        }

        if (is4K && mode.RefreshRateHz >= 60)
        {
            return VideoCableClass.UsbC31Gen2;
        }

        return VideoCableClass.UsbC31Gen1;
    }
}
