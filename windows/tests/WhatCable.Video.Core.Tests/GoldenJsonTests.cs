using System.IO;
using System.Text.Json;
using Xunit;

namespace WhatCable.Video.Core.Tests;

public sealed class GoldenJsonTests
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [Fact]
    public void Diagnostic_4K120_CableBottleneck_MatchesGolden()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 60, PixelClockMhz = 594.0 },
            SinkMaxMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 120, PixelClockMhz = 1188.0 },
            AdvertisedCableClass = VideoCableClass.HdmiHighSpeed,
            SourceGpuCaps = new GpuCapabilities
            {
                Vendor = "NVIDIA",
                Model = "GeForce RTX 4090",
                MaxHdmiGen = "2.1",
                MaxDisplayPortGen = "1.4",
                MaxPixelClockMhz = 2000.0
            }
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.Equal(VideoBottleneck.Cable, diagnostic.Bottleneck);
        Assert.Contains("Cable", diagnostic.Verdict);
        Assert.NotNull(diagnostic.RecommendedCableClass);
        Assert.Equal(VideoCableClass.HdmiUltraHighSpeed, diagnostic.RecommendedCableClass);
    }

    [Fact]
    public void Diagnostic_1080p60_Optimal_MatchesGolden()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 60, PixelClockMhz = 148.5 },
            SinkMaxMode = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 60, PixelClockMhz = 148.5 },
            AdvertisedCableClass = VideoCableClass.HdmiStandard
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.Equal(VideoBottleneck.None, diagnostic.Bottleneck);
        Assert.Contains("Optimal", diagnostic.Verdict);
    }

    [Fact]
    public void Diagnostic_8K120_GpuBottleneck_MatchesGolden()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.DisplayPort,
            ActiveMode = new VideoMode { WidthPx = 7680, HeightPx = 4320, RefreshRateHz = 60, PixelClockMhz = 2376.0 },
            SinkMaxMode = new VideoMode { WidthPx = 7680, HeightPx = 4320, RefreshRateHz = 120, PixelClockMhz = 4752.0 },
            AdvertisedCableClass = VideoCableClass.DisplayPort20UHBR20,
            SourceGpuCaps = new GpuCapabilities
            {
                Vendor = "AMD",
                Model = "Radeon RX 7900 XTX",
                MaxHdmiGen = "2.1",
                MaxDisplayPortGen = "2.0",
                MaxPixelClockMhz = 2000.0
            }
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.Equal(VideoBottleneck.Source, diagnostic.Bottleneck);
        Assert.Contains("GPU", diagnostic.Verdict);
    }

    [Fact]
    public void VideoPortSnapshot_SerializesToJson()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 60 },
            SinkMaxMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 60 },
            AdvertisedCableClass = VideoCableClass.HdmiHighSpeed
        };

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);

        Assert.NotNull(json);
        Assert.Contains("\"connectorType\"", json);
        Assert.Contains("\"activeMode\"", json);
        Assert.Contains("1920", json);
        Assert.Contains("1080", json);
    }

    [Fact]
    public void VideoLinkDiagnosticResult_SerializesToJson()
    {
        var diagnostic = new VideoLinkDiagnosticResult
        {
            Bottleneck = VideoBottleneck.Cable,
            Verdict = "Cable is limiting",
            Details = new[] { "Detail 1", "Detail 2" },
            RecommendedCableClass = VideoCableClass.HdmiUltraHighSpeed
        };

        var json = JsonSerializer.Serialize(diagnostic, JsonOptions);

        Assert.NotNull(json);
        Assert.Contains("\"bottleneck\"", json);
        Assert.Contains("\"verdict\"", json);
        Assert.Contains("Cable", json);
    }
}
