namespace WhatCable.Windows.Backend.Power;

/// <summary>Direction of system-wide battery power flow.</summary>
public enum PowerFlowDirection
{
    /// <summary>Battery is neither charging nor discharging appreciably (e.g. AC, fully charged).</summary>
    Idle,

    /// <summary>Battery is charging (drawing power from the adapter).</summary>
    Charging,

    /// <summary>Battery is discharging (running on battery).</summary>
    Discharging,
}

/// <summary>
/// A single system-wide power reading, derived from the WMI <c>BatteryStatus</c> charge/discharge
/// rate. This is a whole-system figure: Windows exposes no public per-port USB-C power telemetry,
/// so a per-port breakdown is intentionally unavailable (documented in <c>PowerMonitor</c>).
/// </summary>
public sealed record BatteryPowerSample
{
    /// <summary>Instantaneous power magnitude in watts (always non-negative).</summary>
    public double Watts { get; init; }

    /// <summary>Whether the battery is charging, discharging, or idle.</summary>
    public PowerFlowDirection Direction { get; init; }
}

/// <summary>
/// Pure conversion from the raw WMI <c>BatteryStatus</c> fields to a <see cref="BatteryPowerSample"/>.
/// Kept free of any WMI/Windows dependency so it is unit-testable on any host.
/// </summary>
public static class BatteryWattageCalculator
{
    /// <summary>
    /// Computes the active power figure. <paramref name="chargeRateMw"/> and
    /// <paramref name="dischargeRateMw"/> are the WMI rates in milliwatts (one of which is zero at
    /// any given time). When both are zero the battery is idle.
    /// </summary>
    public static BatteryPowerSample Compute(bool charging, bool discharging, int chargeRateMw, int dischargeRateMw)
    {
        // Rates are unsigned in WMI; guard against any negative noise just in case.
        var charge = Math.Max(0, chargeRateMw);
        var discharge = Math.Max(0, dischargeRateMw);

        if (charging && charge > 0)
        {
            return new BatteryPowerSample { Watts = charge / 1000.0, Direction = PowerFlowDirection.Charging };
        }

        if (discharging && discharge > 0)
        {
            return new BatteryPowerSample { Watts = discharge / 1000.0, Direction = PowerFlowDirection.Discharging };
        }

        // Fall back to whichever non-zero rate is present even if the flags disagree.
        if (discharge > 0)
        {
            return new BatteryPowerSample { Watts = discharge / 1000.0, Direction = PowerFlowDirection.Discharging };
        }

        if (charge > 0)
        {
            return new BatteryPowerSample { Watts = charge / 1000.0, Direction = PowerFlowDirection.Charging };
        }

        return new BatteryPowerSample { Watts = 0, Direction = PowerFlowDirection.Idle };
    }
}
