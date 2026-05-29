using WhatCable.Windows.Backend.Power;
using Xunit;

namespace WhatCable.Windows.Backend.Tests;

public sealed class BatteryWattageCalculatorTests
{
    [Fact]
    public void Compute_Charging_ReturnsChargeWatts()
    {
        var sample = BatteryWattageCalculator.Compute(charging: true, discharging: false, chargeRateMw: 25000, dischargeRateMw: 0);

        Assert.Equal(PowerFlowDirection.Charging, sample.Direction);
        Assert.Equal(25.0, sample.Watts);
    }

    [Fact]
    public void Compute_Discharging_ReturnsDischargeWatts()
    {
        var sample = BatteryWattageCalculator.Compute(charging: false, discharging: true, chargeRateMw: 0, dischargeRateMw: 12500);

        Assert.Equal(PowerFlowDirection.Discharging, sample.Direction);
        Assert.Equal(12.5, sample.Watts);
    }

    [Fact]
    public void Compute_BothZero_ReturnsIdle()
    {
        var sample = BatteryWattageCalculator.Compute(charging: false, discharging: false, chargeRateMw: 0, dischargeRateMw: 0);

        Assert.Equal(PowerFlowDirection.Idle, sample.Direction);
        Assert.Equal(0, sample.Watts);
    }

    [Fact]
    public void Compute_FlagsDisagree_FallsBackToNonZeroRate()
    {
        // Flags say idle, but a discharge rate is present: trust the rate.
        var sample = BatteryWattageCalculator.Compute(charging: false, discharging: false, chargeRateMw: 0, dischargeRateMw: 8000);

        Assert.Equal(PowerFlowDirection.Discharging, sample.Direction);
        Assert.Equal(8.0, sample.Watts);
    }

    [Fact]
    public void Compute_NegativeNoise_ClampedToZero()
    {
        var sample = BatteryWattageCalculator.Compute(charging: true, discharging: false, chargeRateMw: -5000, dischargeRateMw: 0);

        Assert.Equal(PowerFlowDirection.Idle, sample.Direction);
        Assert.Equal(0, sample.Watts);
    }
}
