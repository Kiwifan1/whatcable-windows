using WhatCable.Core;
using WhatCable.Windows.App.Core.Services;
using WhatCable.Windows.App.Core.ViewModels;
using Xunit;
using static WhatCable.Windows.App.Core.Tests.SnapshotFactory;

namespace WhatCable.Windows.App.Core.Tests;

public sealed class SnapshotChangeDetectorTests
{
    [Fact]
    public void Detect_NoPreviousSnapshot_ReturnsNoChanges()
    {
        var current = Snapshot(null, Port("Port 1", active: true, Device("Keyboard")));

        Assert.Empty(SnapshotChangeDetector.Detect(null, current));
    }

    [Fact]
    public void Detect_DeviceAdded_ReturnsDeviceConnected()
    {
        var before = Snapshot(null, Port("Port 1", active: false));
        var after = Snapshot(null, Port("Port 1", active: true, Device("Keyboard")));

        var changes = SnapshotChangeDetector.Detect(before, after);

        var change = Assert.Single(changes);
        Assert.Equal(PortChangeKind.DeviceConnected, change.Kind);
        Assert.Equal("Port 1", change.PortName);
    }

    [Fact]
    public void Detect_DeviceRemoved_ReturnsDeviceDisconnected()
    {
        var before = Snapshot(null, Port("Port 1", active: true, Device("Keyboard")));
        var after = Snapshot(null, Port("Port 1", active: false));

        var change = Assert.Single(SnapshotChangeDetector.Detect(before, after));
        Assert.Equal(PortChangeKind.DeviceDisconnected, change.Kind);
    }

    [Fact]
    public void Detect_ChargerConnectedAndDisconnected()
    {
        var noAdapter = Snapshot(null, Port("Port 1", active: false));
        var withAdapter = Snapshot(new AdapterSnapshot { Watts = 65, Source = "AC" }, Port("Port 1", active: false));

        var connected = Assert.Single(SnapshotChangeDetector.Detect(noAdapter, withAdapter));
        Assert.Equal(PortChangeKind.ChargerConnected, connected.Kind);

        var disconnected = Assert.Single(SnapshotChangeDetector.Detect(withAdapter, noAdapter));
        Assert.Equal(PortChangeKind.ChargerDisconnected, disconnected.Kind);
    }

    [Fact]
    public void Detect_NestedDeviceTree_CountsAllNodes()
    {
        var before = Snapshot(null, Port("Port 1", active: true, Device("Hub")));
        var after = Snapshot(null, Port("Port 1", active: true, Device("Hub", Device("Mouse"))));

        var change = Assert.Single(SnapshotChangeDetector.Detect(before, after));
        Assert.Equal(PortChangeKind.DeviceConnected, change.Kind);
    }
}
