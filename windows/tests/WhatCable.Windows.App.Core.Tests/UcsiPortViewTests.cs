using WhatCable.Core;
using WhatCable.Windows.App.Core.Pro;
using Xunit;

namespace WhatCable.Windows.App.Core.Tests;

public sealed class UcsiPortViewTests
{
    [Fact]
    public void FromPort_NoUcsi_ReturnsNull()
    {
        var port = SnapshotFactory.Port("Port 1", active: true);
        Assert.Null(UcsiPortView.FromPort(port));
    }

    [Fact]
    public void FromPort_ParsesCoreFields()
    {
        var ucsi = UcsiJson.Build(
            versionBcd: 0x0200,
            connected: true,
            usb3: true,
            liquidDetected: false,
            liquidDetectionSupported: true,
            cableSpeed: (int)CableSpeed.Usb32Gen2);
        var view = UcsiPortView.FromPort(UcsiJson.Port("Port 1", true, ucsi));

        Assert.NotNull(view);
        Assert.True(view!.Connected);
        Assert.True(view.Usb3Capable);
        Assert.True(view.IsUcsi20);
        Assert.True(view.LiquidDetectionSupported);
        Assert.False(view.LiquidDetected);
        Assert.Equal(CableSpeed.Usb32Gen2, view.CableSpeed);
    }

    [Fact]
    public void FromPort_DetectsActiveAlternateModes()
    {
        var ucsi = UcsiJson.Build(alternateModes: new[]
        {
            (UcsiPortView.DisplayPortSvid, 0u, "DisplayPort", true),
            (UcsiPortView.ThunderboltSvid, 0u, "Thunderbolt", false),
        });
        var view = UcsiPortView.FromPort(UcsiJson.Port("Port 1", true, ucsi));

        Assert.NotNull(view);
        Assert.True(view!.DisplayPortActive);
        Assert.False(view.ThunderboltActive);
        Assert.Equal(2, view.AlternateModes.Count);
    }

    [Fact]
    public void IsUcsi20_FalseForOlderVersion()
    {
        var ucsi = UcsiJson.Build(versionBcd: 0x0102);
        var view = UcsiPortView.FromPort(UcsiJson.Port("Port 1", true, ucsi));

        Assert.NotNull(view);
        Assert.False(view!.IsUcsi20);
    }
}
