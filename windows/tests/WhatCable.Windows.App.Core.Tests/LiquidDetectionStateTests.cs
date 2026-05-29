using WhatCable.Windows.App.Core.Pro;
using Xunit;
using static WhatCable.Windows.App.Core.Tests.SnapshotFactory;

namespace WhatCable.Windows.App.Core.Tests;

public sealed class LiquidDetectionStateTests
{
    [Fact]
    public void FromSnapshot_Null_ReportsUnsupported()
    {
        var state = LiquidDetectionState.FromSnapshot(null);

        Assert.False(state.Supported);
        Assert.False(state.Detected);
        Assert.Equal(LiquidDetectionState.ReasonUnsupported, state.UnavailableReason);
    }

    [Fact]
    public void FromSnapshot_NoUcsi_ReportsUnsupported()
    {
        var snapshot = Snapshot(null, Port("Port 1", active: true));
        var state = LiquidDetectionState.FromSnapshot(snapshot);

        Assert.False(state.Supported);
        Assert.Equal(LiquidDetectionState.ReasonUnsupported, state.UnavailableReason);
    }

    [Fact]
    public void FromSnapshot_SupportedButDry_NoDetection()
    {
        var snapshot = Snapshot(null,
            UcsiJson.Port("Port 1", true, UcsiJson.Build(liquidDetectionSupported: true, liquidDetected: false)));
        var state = LiquidDetectionState.FromSnapshot(snapshot);

        Assert.True(state.Supported);
        Assert.False(state.Detected);
        Assert.Empty(state.AffectedPorts);
        Assert.Null(state.UnavailableReason);
    }

    [Fact]
    public void FromSnapshot_DetectedOnSecondPort_ReportsAffectedIndex()
    {
        var snapshot = Snapshot(null,
            UcsiJson.Port("Port 1", true, UcsiJson.Build(liquidDetectionSupported: true, liquidDetected: false)),
            UcsiJson.Port("Port 2", true, UcsiJson.Build(liquidDetectionSupported: true, liquidDetected: true)));
        var state = LiquidDetectionState.FromSnapshot(snapshot);

        Assert.True(state.Supported);
        Assert.True(state.Detected);
        Assert.Equal(new[] { 2 }, state.AffectedPorts);
    }
}
