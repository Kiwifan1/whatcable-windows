using Xunit;

namespace WhatCable.Video.Core.Tests;

public sealed class VideoLinkDiagnosticTests
{
    [Fact]
    public void Analyze_NullSnapshot_ReturnsUnknown()
    {
        var diagnostic = VideoLinkDiagnostic.Analyze(null);

        Assert.NotNull(diagnostic);
        Assert.Equal(VideoBottleneck.Unknown, diagnostic.Bottleneck);
        Assert.Contains("No video port information available", diagnostic.Verdict);
    }

    [Fact]
    public void Analyze_NoActiveMode_ReturnsCableBottleneck()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = null,
            SinkMaxMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 60 }
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.NotNull(diagnostic);
        Assert.Equal(VideoBottleneck.Cable, diagnostic.Bottleneck);
        Assert.Contains(diagnostic.Details, d => d.Contains("No active video signal detected"));
    }

    [Fact]
    public void Analyze_RunningAtMaxCapability_ReturnsOptimal()
    {
        var mode = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 60 };
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = mode,
            SinkMaxMode = mode,
            AdvertisedCableClass = VideoCableClass.HdmiStandard
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.NotNull(diagnostic);
        Assert.Equal(VideoBottleneck.None, diagnostic.Bottleneck);
        Assert.Contains("Optimal", diagnostic.Verdict);
    }

    [Fact]
    public void Analyze_CableLimiting_ReturnsCableBottleneck()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 30 },
            SinkMaxMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 120 },
            AdvertisedCableClass = VideoCableClass.HdmiStandard, // HDMI 1.4 - max 4K30
            SourceGpuCaps = new GpuCapabilities { MaxHdmiGen = "2.1" } // GPU supports HDMI 2.1
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.NotNull(diagnostic);
        Assert.Equal(VideoBottleneck.Cable, diagnostic.Bottleneck);
        Assert.Contains("Cable", diagnostic.Verdict);
        Assert.NotNull(diagnostic.RecommendedCableClass);
    }

    [Fact]
    public void Analyze_GpuLimiting_ReturnsSourceBottleneck()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 60 },
            SinkMaxMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 120 },
            AdvertisedCableClass = VideoCableClass.HdmiUltraHighSpeed, // HDMI 2.1 cable
            SourceGpuCaps = new GpuCapabilities { MaxHdmiGen = "2.0" } // GPU only supports HDMI 2.0
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.NotNull(diagnostic);
        Assert.Equal(VideoBottleneck.Source, diagnostic.Bottleneck);
        Assert.Contains("GPU", diagnostic.Verdict);
    }

    [Fact]
    public void Analyze_4K120Mode_RequiresHdmi21Cable()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 60 },
            SinkMaxMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 120 },
            AdvertisedCableClass = VideoCableClass.HdmiHighSpeed // HDMI 2.0
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.NotNull(diagnostic);
        Assert.Equal(VideoCableClass.HdmiUltraHighSpeed, diagnostic.RecommendedCableClass);
    }

    [Fact]
    public void Analyze_1080pMode_HdmiStandardSufficient()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 60 },
            SinkMaxMode = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 60 }
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.NotNull(diagnostic);
        // For 1080p60, HDMI Standard (1.4) is sufficient
        Assert.True(diagnostic.RecommendedCableClass == null ||
                   diagnostic.RecommendedCableClass == VideoCableClass.HdmiStandard);
    }

    [Fact]
    public void Analyze_DisplayPortUHBR_SupportsHighBandwidth()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.DisplayPort,
            ActiveMode = new VideoMode { WidthPx = 7680, HeightPx = 4320, RefreshRateHz = 60 },
            SinkMaxMode = new VideoMode { WidthPx = 7680, HeightPx = 4320, RefreshRateHz = 60 },
            AdvertisedCableClass = VideoCableClass.DisplayPort20Uhbr20
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.NotNull(diagnostic);
        // Should be optimal with DP 2.0 UHBR20 for 8K60
        Assert.Equal(VideoBottleneck.None, diagnostic.Bottleneck);
    }

    [Fact]
    public void Analyze_DisplayPortBothLimiting_RecommendsDisplayPortCableClass()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.DisplayPort,
            ActiveMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 60, PixelClockMhz = 594.0 },
            SinkMaxMode = new VideoMode { WidthPx = 7680, HeightPx = 4320, RefreshRateHz = 120, PixelClockMhz = 4752.0 },
            AdvertisedCableClass = VideoCableClass.DisplayPort20Uhbr20,
            SourceGpuCaps = new GpuCapabilities { Vendor = "AMD", Model = "Radeon RX 7900 XTX", MaxPixelClockMhz = 1500.0 }
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.NotNull(diagnostic);
        Assert.Equal(VideoBottleneck.Source, diagnostic.Bottleneck);
        Assert.Equal(VideoCableClass.DisplayPort20Uhbr20, diagnostic.RecommendedCableClass);
    }

    [Fact]
    public void Analyze_IncludesActiveAndMaxModeInDetails()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 60 },
            SinkMaxMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 60 }
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.NotNull(diagnostic);
        Assert.NotEmpty(diagnostic.Details);
        Assert.Contains(diagnostic.Details, d => d.Contains("1920×1080"));
        Assert.Contains(diagnostic.Details, d => d.Contains("3840×2160"));
    }

    [Fact]
    public void Analyze_OnlyOneComponentCapabilityKnown_ReturnsUnknownNotSink()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 60 },
            SinkMaxMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 120 },
            AdvertisedCableClass = VideoCableClass.HdmiUltraHighSpeed,
            SourceGpuCaps = null
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.NotNull(diagnostic);
        Assert.Equal(VideoBottleneck.Unknown, diagnostic.Bottleneck);
        Assert.Contains("Cable or source is limiting", diagnostic.Verdict);
    }

    [Fact]
    public void Analyze_VendorLinkCarriesSinkMax_UpgradesUnknownToSink()
    {
        // A vendor GPU SDK reports a fast measured link (UHBR20 ×4) that can carry the sink's
        // maximum, so the lower active mode is an intentional selection rather than a cable or
        // source limit -- the verdict upgrades from "cable or source limited" (Unknown) to Sink.
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.DisplayPort,
            ActiveMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 60 },
            SinkMaxMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 120 },
            VendorGpuLink = new VendorGpuLinkInfo
            {
                Vendor = "NVIDIA",
                DpLinkRateGbps = 20.0,
                DpLaneCount = 4,
            }
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.Equal(VideoBottleneck.Sink, diagnostic.Bottleneck);
        Assert.Contains(diagnostic.Details, d => d.Contains("can carry the display's maximum", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(diagnostic.Details, d => d.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_VendorLinkBelowSinkMax_UpgradesUnknownToCable()
    {
        // The measured link (HBR2 ×2) cannot carry the sink's maximum, so the diagnostic can
        // confidently attribute the bottleneck to the cable/link rather than reporting Unknown.
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.DisplayPort,
            ActiveMode = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 60 },
            SinkMaxMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 120 },
            VendorGpuLink = new VendorGpuLinkInfo
            {
                Vendor = "AMD",
                DpLinkRateGbps = 5.4,
                DpLaneCount = 2,
            }
        };

        var diagnostic = VideoLinkDiagnostic.Analyze(snapshot);

        Assert.Equal(VideoBottleneck.Cable, diagnostic.Bottleneck);
        Assert.NotNull(diagnostic.RecommendedCableClass);
        Assert.Contains(diagnostic.Details, d => d.Contains("AMD", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TotalLinkBandwidthGbps_DerivesFromDpAndHdmiFields()
    {
        Assert.Equal(32.4, new VendorGpuLinkInfo { DpLinkRateGbps = 8.1, DpLaneCount = 4 }.TotalLinkBandwidthGbps);
        Assert.Equal(48.0, new VendorGpuLinkInfo { HdmiFrlRateGbps = 12.0, HdmiFrlLaneCount = 4 }.TotalLinkBandwidthGbps);
        Assert.Equal(40.0, new VendorGpuLinkInfo { NegotiatedLinkRateGbps = 40.0 }.TotalLinkBandwidthGbps);
        Assert.Null(new VendorGpuLinkInfo { Vendor = "Intel" }.TotalLinkBandwidthGbps);
    }
}
