namespace WhatCable.Windows.Backend.Power;

/// <summary>
/// Supplies live system-wide power samples for the Pro Power Monitor. The Windows implementation
/// reads the WMI <c>BatteryStatus</c> class; on a desktop with no battery the source reports itself
/// unavailable with a machine-readable reason.
/// </summary>
public interface IBatteryWattageSource
{
    /// <summary>True when a battery is present and the WMI rate fields can be read.</summary>
    bool IsAvailable { get; }

    /// <summary>Machine-readable reason when <see cref="IsAvailable"/> is false (e.g. <c>no_battery</c>).</summary>
    string? UnavailableReason { get; }

    /// <summary>Reads the current sample, or null when unavailable.</summary>
    BatteryPowerSample? Read();
}
