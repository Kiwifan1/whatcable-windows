using WhatCable.Windows.App.Core.Services;
using WhatCable.Windows.App.Core.ViewModels;
using Xunit;

namespace WhatCable.Windows.App.Core.Tests;

public sealed class PowerMonitorViewModelTests
{
    private static PowerReading Reading(double watts, PowerFlow flow = PowerFlow.Discharging)
        => new() { Watts = watts, Flow = flow };

    [Fact]
    public void Sample_ProLocked_StaysInUpsellState()
    {
        var source = new FakePowerMonitorSource(isAvailable: true, readings: Reading(12));
        var vm = new PowerMonitorViewModel(source, proUnlocked: false);

        vm.Sample();

        Assert.True(vm.ShowUpsell);
        Assert.False(vm.IsProUnlocked);
        Assert.False(vm.HasData);
    }

    [Fact]
    public void Sample_ProUnlocked_RecordsReading()
    {
        var source = new FakePowerMonitorSource(isAvailable: true, readings: Reading(12.5, PowerFlow.Charging));
        var vm = new PowerMonitorViewModel(source, proUnlocked: true);

        vm.Sample();

        Assert.True(vm.HasData);
        Assert.Equal(12.5, vm.CurrentWatts);
        Assert.Equal(12.5, vm.PeakWatts);
        Assert.Equal(PowerFlow.Charging, vm.Flow);
        Assert.False(vm.ShowUpsell);
    }

    [Fact]
    public void Sample_NoSourceAndUnlocked_ReportsUnavailable()
    {
        var vm = new PowerMonitorViewModel(source: null, proUnlocked: true);

        vm.Sample();

        Assert.True(vm.IsUnavailable);
        Assert.Equal("power_monitor_unavailable", vm.UnavailableReason);
    }

    [Fact]
    public void Sample_NoBattery_SurfacesSourceReason()
    {
        var source = new FakePowerMonitorSource(isAvailable: false, unavailableReason: "no_battery");
        var vm = new PowerMonitorViewModel(source, proUnlocked: true);

        vm.Sample();

        Assert.True(vm.IsUnavailable);
        Assert.Equal("no_battery", vm.UnavailableReason);
    }

    [Fact]
    public void Sample_RollingWindow_DropsOldest()
    {
        var source = new FakePowerMonitorSource(
            isAvailable: true,
            readings: new PowerReading?[] { Reading(1), Reading(2), Reading(3) });
        var vm = new PowerMonitorViewModel(source, proUnlocked: true, capacity: 2);

        vm.Sample();
        vm.Sample();
        vm.Sample();

        Assert.Equal(2, vm.Samples.Count);
        Assert.Equal(new double[] { 2, 3 }, vm.Samples);
    }

    [Fact]
    public void GetPlotPoints_EmptyWhenNoData()
    {
        var vm = new PowerMonitorViewModel(source: null, proUnlocked: true);
        Assert.Empty(vm.GetPlotPoints(100, 50));
    }

    [Fact]
    public void GetPlotPoints_MapsSamplesIntoBox()
    {
        var source = new FakePowerMonitorSource(
            isAvailable: true,
            readings: new PowerReading?[] { Reading(5), Reading(10) });
        var vm = new PowerMonitorViewModel(source, proUnlocked: true);
        vm.Sample();
        vm.Sample();

        var points = vm.GetPlotPoints(100, 50);

        Assert.Equal(2, points.Count);
        Assert.Equal(0, points[0].X);
        Assert.Equal(100, points[1].X);
        // Peak (10W) sits at the top of the box (y == 0); 5W is halfway down.
        Assert.Equal(0, points[1].Y, 3);
        Assert.Equal(25, points[0].Y, 3);
    }

    [Fact]
    public void PerPort_AlwaysUnavailable()
    {
        var vm = new PowerMonitorViewModel(source: null, proUnlocked: true);

        Assert.False(vm.PerPortAvailable);
        Assert.Equal("windows_no_per_port_power_api", vm.PerPortUnavailableReason);
    }
}
