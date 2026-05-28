using WhatCable.Windows.Backend.Power;
using Xunit;

namespace WhatCable.Windows.Backend.Tests;

public sealed class PowerAdapterTests
{
    private static string Banner(SystemPowerInfo info)
        => new PowerAdapter(new FakeSystemPowerSource(info)).GetBatteryBanner();

    [Fact]
    public void Banner_NoBatteryOnAc_ReportsDesktopSystem()
    {
        var info = new SystemPowerInfo { AcOnline = true, BatteryPresent = false };
        Assert.Equal(
            "On AC power — no battery detected (desktop or AC-only system).",
            Banner(info));
    }

    [Fact]
    public void Banner_Charging_IncludesPercentAndState()
    {
        var info = new SystemPowerInfo
        {
            AcOnline = true,
            BatteryPresent = true,
            Charging = true,
            BatteryPercent = 80,
        };
        Assert.Equal("Battery 80% — charging (AC connected).", Banner(info));
    }

    [Fact]
    public void Banner_FullOnAc_ReportsFullyCharged()
    {
        var info = new SystemPowerInfo
        {
            AcOnline = true,
            BatteryPresent = true,
            Charging = false,
            BatteryPercent = 100,
        };
        Assert.Equal("Battery 100% — fully charged (AC connected).", Banner(info));
    }

    [Fact]
    public void Banner_OnBattery_IncludesRemainingTime()
    {
        var info = new SystemPowerInfo
        {
            AcOnline = false,
            BatteryPresent = true,
            BatteryPercent = 55,
            RemainingMinutes = 125,
        };
        Assert.Equal("Battery 55% — on battery, ~2 h 5 min remaining.", Banner(info));
    }

    [Fact]
    public void Banner_UnknownPercent_OmitsNumber()
    {
        var info = new SystemPowerInfo { AcOnline = false, BatteryPresent = true };
        Assert.Equal("Battery — on battery.", Banner(info));
    }

    [Fact]
    public void GetAdapter_ReportsSourceAndBanner()
    {
        var source = new FakeSystemPowerSource(new SystemPowerInfo
        {
            AcOnline = true,
            BatteryPresent = true,
            Charging = true,
            BatteryPercent = 42,
        });
        var adapter = new PowerAdapter(source);

        var snapshot = adapter.GetAdapter();

        Assert.Equal("AC", snapshot.Source);
        Assert.Equal("Battery 42% — charging (AC connected).", snapshot.Description);
        Assert.Equal(false, snapshot.IsWireless);
    }

    [Fact]
    public void GetAdapter_OnBattery_ReportsBatterySource()
    {
        var source = new FakeSystemPowerSource(new SystemPowerInfo
        {
            AcOnline = false,
            BatteryPresent = true,
            BatteryPercent = 30,
            RemainingMinutes = 45,
        });

        Assert.Equal("Battery", new PowerAdapter(source).GetAdapter().Source);
    }
}
