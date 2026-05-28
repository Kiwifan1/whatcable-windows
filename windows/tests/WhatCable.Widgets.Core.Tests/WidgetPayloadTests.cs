using WhatCable.Core;
using Xunit;
using static WhatCable.Widgets.Core.Tests.SnapshotFactory;

namespace WhatCable.Widgets.Core.Tests;

public sealed class WidgetPayloadTests
{
    [Fact]
    public void RoundTrips_SnapshotAndVerdicts()
    {
        var payload = new WidgetPayload
        {
            Snapshot = Snapshot(new AdapterSnapshot { Watts = 100 },
                Port("Port 1", active: true, headline: "Dock", cable: Cable("40 Gbps"))),
            VideoVerdicts = new[] { new VideoVerdict("Monitor", "OK") },
        };

        var restored = WidgetPayload.FromJson(payload.ToJson());

        Assert.Equal(100, restored.Snapshot.Adapter?.Watts);
        Assert.Single(restored.Snapshot.Ports);
        Assert.Equal("Port 1", restored.Snapshot.Ports[0].Name);
        Assert.Equal("40 Gbps", restored.Snapshot.Ports[0].Cable?.Speed);
        Assert.Single(restored.VideoVerdicts);
        Assert.Equal("Monitor", restored.VideoVerdicts[0].DisplayName);
        Assert.Equal("OK", restored.VideoVerdicts[0].Verdict);
    }

    [Fact]
    public void ToJson_IsSingleLine()
    {
        var json = new WidgetPayload { Snapshot = Snapshot(null, Port("Port 1", active: false)) }.ToJson();

        Assert.DoesNotContain('\n', json);
    }

    [Fact]
    public void FromJson_Malformed_Throws()
    {
        Assert.ThrowsAny<Exception>(() => WidgetPayload.FromJson("{ not json"));
    }
}
