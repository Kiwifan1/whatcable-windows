using WhatCable.Core;
using WhatCable.Windows.App.Core.Localization;
using WhatCable.Windows.App.Core.ViewModels;
using Xunit;

namespace WhatCable.Windows.App.Core.Tests;

public sealed class PortViewModelTests
{
    private static ILocalizer Localizer() => new DictionaryLocalizer(new Dictionary<string, string>
    {
        [StringKeys.TrayNothingConnected] = "Nothing connected",
    });

    [Fact]
    public void Projects_DeviceTree_PowerProfiles_And_TrustSignals()
    {
        var port = new Port
        {
            Name = "Port 1",
            ConnectionActive = true,
            Headline = "USB4 device",
            Subtitle = "40 Gbps",
            Status = "Active",
            Device = new DeviceNode
            {
                Name = "Hub",
                Speed = "SuperSpeed",
                Children = new[] { new DeviceNode { Name = "Mouse", Speed = "Full" } },
            },
            PowerSources = new[]
            {
                new ChargerProfile { Name = "USB-C Charger", MaxPowerW = 65 },
            },
            Cable = new Cable
            {
                Endpoint = "host",
                VendorName = "Acme",
                TrustFlags = new[] { new TrustFlag("emarker", "E-marked", "Active electronics") },
            },
        };

        var vm = new PortViewModel(port, Localizer());

        Assert.Equal("Port 1", vm.Name);
        Assert.Equal("USB4 device", vm.Headline);
        Assert.Equal("40 Gbps", vm.Subtitle);
        Assert.True(vm.IsConnected);
        Assert.True(vm.HasDevices);
        Assert.Single(vm.Devices);
        Assert.Single(vm.Devices[0].Children);
        Assert.True(vm.HasPowerProfiles);
        Assert.True(vm.HasTrustSignals);
        Assert.True(vm.HasCable);
        Assert.False(vm.IsUnavailable);
    }

    [Fact]
    public void EmptyPort_FallsBackToLocalisedHeadline_AndHasNoSections()
    {
        var port = new Port { Name = "Port 2", ConnectionActive = false, UnavailableReason = "ucsi_not_supported" };

        var vm = new PortViewModel(port, Localizer());

        Assert.Equal("Nothing connected", vm.Headline);
        Assert.False(vm.IsConnected);
        Assert.False(vm.HasDevices);
        Assert.False(vm.HasPowerProfiles);
        Assert.False(vm.HasTrustSignals);
        Assert.False(vm.HasCable);
        Assert.True(vm.IsUnavailable);
        Assert.Equal("ucsi_not_supported", vm.UnavailableReason);
    }
}
