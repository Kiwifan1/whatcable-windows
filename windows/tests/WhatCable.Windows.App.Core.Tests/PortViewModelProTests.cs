using WhatCable.Windows.App.Core.Localization;
using WhatCable.Windows.App.Core.Pro;
using WhatCable.Windows.App.Core.ViewModels;
using Xunit;

namespace WhatCable.Windows.App.Core.Tests;

public sealed class PortViewModelProTests
{
    private static ILocalizer Localizer() => new DictionaryLocalizer(new Dictionary<string, string>
    {
        [StringKeys.TrayNothingConnected] = "Nothing connected",
    });

    [Fact]
    public void NonUcsiPort_NoPinDiagram()
    {
        var port = SnapshotFactory.Port("Port 1", active: true);
        var vm = new PortViewModel(port, Localizer(), proUnlocked: true);

        Assert.False(vm.SupportsPinDiagram);
        Assert.False(vm.ShowPinDiagram);
        Assert.False(vm.ShowPinDiagramUpsell);
    }

    [Fact]
    public void UcsiPort_ProLocked_ShowsUpsellNoDiagram()
    {
        var port = UcsiJson.Port("Port 1", true, UcsiJson.Build(connected: true, usb3: true));
        var vm = new PortViewModel(port, Localizer(), proUnlocked: false);

        Assert.True(vm.SupportsPinDiagram);
        Assert.False(vm.ShowPinDiagram);
        Assert.True(vm.ShowPinDiagramUpsell);
        Assert.Null(vm.PinDiagramSvg);
    }

    [Fact]
    public void UcsiPort_ProUnlocked_RendersDiagram()
    {
        var port = UcsiJson.Port("Port 1", true, UcsiJson.Build(connected: true, usb3: true));
        var vm = new PortViewModel(port, Localizer(), proUnlocked: true);

        Assert.True(vm.ShowPinDiagram);
        Assert.False(vm.ShowPinDiagramUpsell);
        Assert.NotNull(vm.PinDiagramSvg);
        Assert.Contains("<svg", vm.PinDiagramSvg);
    }

    [Fact]
    public void UcsiPort_ProUnlocked_NotConnected_ReportsUnavailableReason()
    {
        var port = UcsiJson.Port("Port 1", false, UcsiJson.Build(connected: false));
        var vm = new PortViewModel(port, Localizer(), proUnlocked: true);

        Assert.False(vm.ShowPinDiagram);
        Assert.Equal(PinDiagramRenderer.ReasonNotConnected, vm.PinDiagramUnavailableReason);
    }
}
