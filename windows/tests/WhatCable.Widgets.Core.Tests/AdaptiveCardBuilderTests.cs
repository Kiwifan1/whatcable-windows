using System.Text.Json;
using System.Text.Json.Nodes;
using WhatCable.Core;
using Xunit;
using static WhatCable.Widgets.Core.Tests.SnapshotFactory;

namespace WhatCable.Widgets.Core.Tests;

public sealed class AdaptiveCardBuilderTests
{
    private static WidgetPayload Payload(Snapshot snapshot, params VideoVerdict[] verdicts)
        => new() { Snapshot = snapshot, VideoVerdicts = verdicts };

    private static JsonObject Parse(string json)
        => (JsonObject)JsonNode.Parse(json)!;

    [Theory]
    [InlineData(WidgetSize.Small)]
    [InlineData(WidgetSize.Medium)]
    [InlineData(WidgetSize.Large)]
    public void Build_ProducesValidAdaptiveCardEnvelope(WidgetSize size)
    {
        var snapshot = Snapshot(new AdapterSnapshot { Watts = 65 },
            Port("Port 1", active: true, headline: "USB4 dock", cable: Cable("40 Gbps")));

        var card = Parse(AdaptiveCardBuilder.Build(Payload(snapshot), size));

        Assert.Equal("AdaptiveCard", (string?)card["type"]);
        Assert.Equal("1.5", (string?)card["version"]);
        Assert.NotNull(card["$schema"]);
        Assert.IsType<JsonArray>(card["body"]);
        Assert.NotEmpty((JsonArray)card["body"]!);
    }

    [Fact]
    public void Build_Small_ShowsChargingAndSpeed()
    {
        var snapshot = Snapshot(new AdapterSnapshot { Watts = 65 },
            Port("Port 1", active: true, cable: Cable("40 Gbps")));

        var json = AdaptiveCardBuilder.Build(Payload(snapshot), WidgetSize.Small);

        Assert.Contains("Charging · 65 W", json);
        Assert.Contains("40 Gbps", json);
    }

    [Fact]
    public void Build_Medium_ListsEachPort()
    {
        var snapshot = Snapshot(null,
            Port("Port 1", active: true, headline: "Display"),
            Port("Port 2", active: false));

        var json = AdaptiveCardBuilder.Build(Payload(snapshot), WidgetSize.Medium);

        Assert.Contains("Port 1", json);
        Assert.Contains("Port 2", json);
    }

    [Fact]
    public void Build_Medium_NoPorts_ShowsEmptyMessage()
    {
        var json = AdaptiveCardBuilder.Build(Payload(Snapshot(null)), WidgetSize.Medium);

        Assert.Contains("No USB-C ports detected", json);
    }

    [Fact]
    public void Build_Large_IncludesVideoVerdicts()
    {
        var snapshot = Snapshot(null, Port("Port 1", active: true));
        var payload = Payload(snapshot, new VideoVerdict("Dell U2720Q", "4K60 link OK"));

        var json = AdaptiveCardBuilder.Build(payload, WidgetSize.Large);

        Assert.Contains("Video diagnostics", json);
        Assert.Contains("Dell U2720Q", json);
        Assert.Contains("4K60 link OK", json);
    }

    [Fact]
    public void Build_Large_NoVideo_OmitsVideoSection()
    {
        var json = AdaptiveCardBuilder.Build(Payload(Snapshot(null, Port("Port 1", active: false))), WidgetSize.Large);

        Assert.DoesNotContain("Video diagnostics", json);
    }

    [Fact]
    public void Build_NullPayload_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AdaptiveCardBuilder.Build(null!, WidgetSize.Small));
    }
}
