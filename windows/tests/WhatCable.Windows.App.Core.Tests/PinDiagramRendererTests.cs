using WhatCable.Core;
using WhatCable.Windows.App.Core.Pro;
using Xunit;

namespace WhatCable.Windows.App.Core.Tests;

public sealed class PinDiagramRendererTests
{
    [Fact]
    public void Render_NullView_UnavailableNotUcsi()
    {
        var diagram = PinDiagramRenderer.Render(null);

        Assert.False(diagram.Available);
        Assert.Equal(PinDiagramRenderer.ReasonNotUcsi, diagram.UnavailableReason);
    }

    [Fact]
    public void Render_NotConnected_UnavailableNoPartner()
    {
        var view = UcsiPortView.FromPort(UcsiJson.Port("Port 1", false, UcsiJson.Build(connected: false)));
        var diagram = PinDiagramRenderer.Render(view);

        Assert.False(diagram.Available);
        Assert.Equal(PinDiagramRenderer.ReasonNotConnected, diagram.UnavailableReason);
    }

    [Fact]
    public void Render_ThunderboltActive_ClassifiesThunderbolt()
    {
        var view = UcsiPortView.FromPort(UcsiJson.Port("Port 1", true, UcsiJson.Build(
            connected: true,
            alternateModes: new[] { (UcsiPortView.ThunderboltSvid, 0u, "TBT", true) })));
        var diagram = PinDiagramRenderer.Render(view);

        Assert.True(diagram.Available);
        Assert.Equal(CableClass.Thunderbolt, diagram.CableClass);
        Assert.NotNull(diagram.Svg);
        Assert.Contains("<svg", diagram.Svg);
        Assert.NotNull(diagram.PinMap);
    }

    [Fact]
    public void Render_DisplayPortActive_ClassifiesDisplayPort()
    {
        var view = UcsiPortView.FromPort(UcsiJson.Port("Port 1", true, UcsiJson.Build(
            connected: true,
            usb3: true,
            alternateModes: new[] { (UcsiPortView.DisplayPortSvid, 0u, "DP", true) })));
        var diagram = PinDiagramRenderer.Render(view);

        Assert.True(diagram.Available);
        Assert.Equal(CableClass.DisplayPort, diagram.CableClass);
        Assert.Contains("DP", diagram.SignalSummary);
    }

    [Fact]
    public void Render_Usb3Capable_ClassifiesUsb3()
    {
        var view = UcsiPortView.FromPort(UcsiJson.Port("Port 1", true, UcsiJson.Build(connected: true, usb3: true)));
        var diagram = PinDiagramRenderer.Render(view);

        Assert.True(diagram.Available);
        Assert.Equal(CableClass.Usb3, diagram.CableClass);
    }

    [Fact]
    public void Render_Usb2Only_ClassifiesUsb2()
    {
        var view = UcsiPortView.FromPort(UcsiJson.Port("Port 1", true, UcsiJson.Build(connected: true, usb3: false)));
        var diagram = PinDiagramRenderer.Render(view);

        Assert.True(diagram.Available);
        Assert.Equal(CableClass.Usb2Only, diagram.CableClass);
        Assert.Equal("USB 2.0 only", diagram.SignalSummary);
    }

    [Fact]
    public void Render_Svg_IsSelfContainedAndEscaped()
    {
        var view = UcsiPortView.FromPort(UcsiJson.Port("Port 1", true, UcsiJson.Build(connected: true, usb3: true)));
        var diagram = PinDiagramRenderer.Render(view);

        Assert.NotNull(diagram.Svg);
        Assert.StartsWith("<svg", diagram.Svg);
        Assert.EndsWith("</svg>", diagram.Svg);
        Assert.DoesNotContain("href", diagram.Svg);
    }
}
