using System.Linq;
using WhatCable.Video.Core;
using WhatCable.Windows.Backend.Video;
using Xunit;

namespace WhatCable.Windows.Backend.Tests;

/// <summary>
/// Exercises the mapping from recorded <c>QueryDisplayConfig</c> active-mode + EDID pairs
/// onto <see cref="VideoPortReport"/>, including the stock-PC "cable or source limited"
/// verdict described in the issue acceptance criteria.
/// </summary>
public sealed class VideoPortAdapterTests
{
    private static byte[] LoadEdid(string name)
        => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "edid", name));

    [Fact]
    public void GetReports_StockPcUnderdrivenLink_ReportsCableOrSourceLimited()
    {
        // Recorded pair: the 4K120 HDMI 2.1 FRL12 sink (EDID) driven at only 4K60 with no
        // cable e-marker or GPU caps available (the stock-PC case).
        var enumerator = new FakeDisplayEnumerator(new DisplayInfo
        {
            Name = "Acme 4K120",
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 60 },
            EdidRaw = LoadEdid("4k120-hdmi21.bin"),
        });

        var report = Assert.Single(new VideoPortAdapter(enumerator).GetReports());

        Assert.Equal("Acme 4K120", report.DisplayName);
        Assert.Equal(VideoBottleneck.Unknown, report.Diagnostic.Bottleneck);
        Assert.Contains(report.Diagnostic.Details, d => d.Contains("cable or source limited", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Cable or source is limiting", report.Diagnostic.Verdict, StringComparison.OrdinalIgnoreCase);

        // The sink's advertised maximum is derived from the EDID and exceeds the active mode.
        Assert.NotNull(report.Port.SinkMaxMode);
        var sinkScore = (long)report.Port.SinkMaxMode!.WidthPx * report.Port.SinkMaxMode.HeightPx * (long)report.Port.SinkMaxMode.RefreshRateHz;
        var activeScore = (long)report.Port.ActiveMode!.WidthPx * report.Port.ActiveMode.HeightPx * (long)report.Port.ActiveMode.RefreshRateHz;
        Assert.True(sinkScore > activeScore);
    }

    [Fact]
    public void GetReports_RunningAtSinkMaximum_ReportsOptimal()
    {
        // First read the sink's advertised maximum derived from the EDID, then drive the
        // path at exactly that mode and confirm the diagnostic reports an optimal link.
        var probe = new VideoPortAdapter(new FakeDisplayEnumerator(new DisplayInfo
        {
            Name = "Dell U2412M",
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 640, HeightPx = 480, RefreshRateHz = 60 },
            EdidRaw = LoadEdid("1080p60-hdmi14.bin"),
        })).GetReports();
        var sinkMax = Assert.Single(probe).Port.SinkMaxMode;
        Assert.NotNull(sinkMax);

        var enumerator = new FakeDisplayEnumerator(new DisplayInfo
        {
            Name = "Dell U2412M",
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = sinkMax,
            EdidRaw = LoadEdid("1080p60-hdmi14.bin"),
        });

        var report = Assert.Single(new VideoPortAdapter(enumerator).GetReports());

        Assert.Equal(VideoBottleneck.None, report.Diagnostic.Bottleneck);
    }

    [Fact]
    public void GetReports_NoEdid_LeavesSinkMaxModeNull()
    {
        var enumerator = new FakeDisplayEnumerator(new DisplayInfo
        {
            Name = "Unknown",
            ConnectorType = VideoConnectorType.DisplayPort,
            ActiveMode = new VideoMode { WidthPx = 2560, HeightPx = 1440, RefreshRateHz = 144 },
            EdidRaw = null,
        });

        var report = Assert.Single(new VideoPortAdapter(enumerator).GetReports());

        Assert.Null(report.Port.SinkMaxMode);
        Assert.Equal(VideoBottleneck.Unknown, report.Diagnostic.Bottleneck);
    }
}
