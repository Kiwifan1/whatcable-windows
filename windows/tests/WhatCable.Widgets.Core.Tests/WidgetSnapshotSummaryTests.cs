using WhatCable.Core;
using Xunit;
using static WhatCable.Widgets.Core.Tests.SnapshotFactory;

namespace WhatCable.Widgets.Core.Tests;

public sealed class WidgetSnapshotSummaryTests
{
    [Fact]
    public void ChargingStatus_WithWatts_IncludesWattageAndSource()
    {
        var snapshot = Snapshot(new AdapterSnapshot { Watts = 96, Description = "USB-C PD" });

        Assert.Equal("Charging · 96 W · USB-C PD", WidgetSnapshotSummary.ChargingStatus(snapshot));
    }

    [Fact]
    public void ChargingStatus_NoAdapter_ReportsNotCharging()
    {
        Assert.Equal("Not charging", WidgetSnapshotSummary.ChargingStatus(Snapshot(null)));
    }

    [Fact]
    public void ActivePortSpeed_PrefersCableSpeed()
    {
        var snapshot = Snapshot(null,
            Port("Port 1", active: false),
            Port("Port 2", active: true, cable: Cable("40 Gbps"), device: Device("Dock", "10 Gbps")));

        Assert.Equal("40 Gbps", WidgetSnapshotSummary.ActivePortSpeed(snapshot));
    }

    [Fact]
    public void ActivePortSpeed_FallsBackToDeviceSpeed()
    {
        var snapshot = Snapshot(null, Port("Port 1", active: true, device: Device("Disk", "10 Gbps")));

        Assert.Equal("10 Gbps", WidgetSnapshotSummary.ActivePortSpeed(snapshot));
    }

    [Fact]
    public void ActivePortSpeed_NothingConnected_IsEmpty()
    {
        Assert.Equal(string.Empty, WidgetSnapshotSummary.ActivePortSpeed(Snapshot(null, Port("Port 1", active: false))));
    }

    [Fact]
    public void PortRows_MapsActiveAndInactivePorts()
    {
        var snapshot = Snapshot(null,
            Port("Port 1", active: true, headline: "USB4 dock", cable: Cable("40 Gbps")),
            Port("Port 2", active: false));

        var rows = WidgetSnapshotSummary.PortRows(snapshot);

        Assert.Equal(2, rows.Count);
        Assert.True(rows[0].Active);
        Assert.Equal("USB4 dock", rows[0].Status);
        Assert.Equal("40 Gbps", rows[0].Detail);
        Assert.False(rows[1].Active);
        Assert.Equal("Nothing connected", rows[1].Status);
    }
}
