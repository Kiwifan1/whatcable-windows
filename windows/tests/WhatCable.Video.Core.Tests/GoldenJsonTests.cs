using Xunit;

namespace WhatCable.Video.Core.Tests;

public sealed class GoldenJsonTests
{
    [Theory]
    [InlineData("4k120-hdmi21-cable-bottleneck.json")]
    [InlineData("1080p60-hdmi14-optimal.json")]
    [InlineData("8k120-dp20-both-limiting.json")]
    public void Diagnostic_GoldenFixtures_Match(string fixtureName)
    {
        var fixtureContent = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName));
        var fixture = VideoJsonFormatter.Parse<GoldenDiagnosticFixture>(fixtureContent);

        Assert.NotNull(fixture);

        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = fixture!.ConnectorType,
            ActiveMode = fixture.ActiveMode,
            SinkMaxMode = fixture.SinkMaxMode,
            AdvertisedCableClass = fixture.AdvertisedCableClass,
            SourceGpuCaps = fixture.SourceGpuCaps
        };

        var actual = VideoJsonFormatter.Render(VideoLinkDiagnostic.Analyze(snapshot));
        var expectedDiagnostic = VideoJsonFormatter.Render(fixture.Diagnostic);

        Assert.Equal(expectedDiagnostic.Replace("\r\n", "\n"), actual.Replace("\r\n", "\n"));
    }

    [Fact]
    public void VideoPortSnapshot_SerializesToJson()
    {
        var snapshot = new VideoPortSnapshot
        {
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 60 },
            SinkMaxMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 60 },
            AdvertisedCableClass = VideoCableClass.HdmiHighSpeed,
            EdidRaw = new byte[] { 0x01, 0x02, 0x03 }
        };

        var json = VideoJsonFormatter.Render(snapshot);

        Assert.NotNull(json);
        Assert.Contains("\"connectorType\": \"HDMI\"", json);
        Assert.Contains("\"activeMode\"", json);
        Assert.DoesNotContain("edidRaw", json);
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

        var json = VideoJsonFormatter.Render(diagnostic);

        Assert.NotNull(json);
        Assert.Contains("\"bottleneck\": \"Cable\"", json);
        Assert.Contains("\"recommendedCableClass\": \"HdmiUltraHighSpeed\"", json);
    }

    private sealed record GoldenDiagnosticFixture
    {
        public VideoConnectorType ConnectorType { get; init; }
        public VideoMode? ActiveMode { get; init; }
        public VideoMode? SinkMaxMode { get; init; }
        public VideoCableClass? AdvertisedCableClass { get; init; }
        public GpuCapabilities? SourceGpuCaps { get; init; }
        public VideoLinkDiagnosticResult Diagnostic { get; init; } = new();
    }
}
