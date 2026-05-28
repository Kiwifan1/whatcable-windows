using WhatCable.Core;
using Xunit;
using static WhatCable.Widgets.Core.Tests.SnapshotFactory;

namespace WhatCable.Widgets.Core.Tests;

public sealed class WidgetSnapshotPipeTests
{
    [Fact]
    public async Task PublishAsync_NoServer_ReturnsFalse()
    {
        var published = await WidgetSnapshotPipe.PublishAsync(
            new WidgetPayload { Snapshot = Snapshot(null) },
            connectTimeoutMs: 200);

        Assert.False(published);
    }

    [Fact]
    public async Task PublishAsync_DeliversPayloadToServer()
    {
        using var server = new WidgetSnapshotPipeServer();
        var received = new TaskCompletionSource<WidgetPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.PayloadReceived += (_, payload) => received.TrySetResult(payload);
        server.Start();

        var sent = new WidgetPayload
        {
            Snapshot = Snapshot(new AdapterSnapshot { Watts = 45 }, Port("Port 1", active: true)),
            VideoVerdicts = new[] { new VideoVerdict("Display", "OK") },
        };

        bool published = false;
        for (var attempt = 0; attempt < 20 && !published; attempt++)
        {
            published = await WidgetSnapshotPipe.PublishAsync(sent, connectTimeoutMs: 500);
            if (!published)
            {
                await Task.Delay(50);
            }
        }

        Assert.True(published, "Publisher never connected to the server pipe.");

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(received.Task, completed);

        var got = await received.Task;
        Assert.Equal(45, got.Snapshot.Adapter?.Watts);
        Assert.Single(got.VideoVerdicts);
        Assert.Equal("Display", got.VideoVerdicts[0].DisplayName);
    }
}
