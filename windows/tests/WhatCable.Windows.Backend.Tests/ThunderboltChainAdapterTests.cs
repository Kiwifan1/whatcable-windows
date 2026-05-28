using System.Linq;
using WhatCable.Windows.Backend.Thunderbolt;
using Xunit;

namespace WhatCable.Windows.Backend.Tests;

/// <summary>
/// Verifies the Thunderbolt chain mapping, including the documented Windows API limitation
/// that per-lane link speeds are unavailable.
/// </summary>
public sealed class ThunderboltChainAdapterTests
{
    [Fact]
    public void GetChain_AssignsDepthAndMarksPerLaneSpeedsUnavailable()
    {
        var enumerator = new FakeThunderboltEnumerator(
            new ThunderboltDeviceInfo { Name = "TB Controller", InstanceId = "PCI\\TB#0" },
            new ThunderboltDeviceInfo { Name = "Dock", InstanceId = "USB4\\DOCK#1" },
            new ThunderboltDeviceInfo { Name = "Display", InstanceId = "USB4\\DISP#2" });

        var chain = new ThunderboltChainAdapter(enumerator).GetChain();

        Assert.Equal(3, chain.Count);
        Assert.Equal(new[] { 0, 1, 2 }, chain.Select(d => d.ChainDepth).ToArray());
        Assert.All(chain, d => Assert.False(d.PerLaneSpeedsAvailable));
        Assert.All(chain, d => Assert.Equal(ThunderboltChainAdapter.PerLaneUnavailableReason, d.UnavailableReason));
        Assert.Equal("Dock", chain[1].Name);
        Assert.Equal("USB4\\DOCK#1", chain[1].InstanceId);
    }

    [Fact]
    public void GetChain_EmptyEnumeration_ReturnsEmpty()
    {
        var chain = new ThunderboltChainAdapter(new FakeThunderboltEnumerator()).GetChain();

        Assert.Empty(chain);
    }
}
