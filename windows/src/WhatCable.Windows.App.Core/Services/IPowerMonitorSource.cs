namespace WhatCable.Windows.App.Core.Services;

/// <summary>Direction of system-wide battery power flow, mirrored from the backend sample.</summary>
public enum PowerFlow
{
    Idle,
    Charging,
    Discharging,
}

/// <summary>A single system-wide power reading for the Pro Power Monitor.</summary>
public sealed record PowerReading
{
    /// <summary>Instantaneous power magnitude in watts (non-negative).</summary>
    public double Watts { get; init; }

    /// <summary>Whether the battery is charging, discharging, or idle.</summary>
    public PowerFlow Flow { get; init; }
}

/// <summary>
/// Supplies live system-wide power readings to the Pro Power Monitor graph. The WinUI app implements
/// this over <c>WhatCable.Windows.Backend</c>'s WMI source; tests use a fake.
/// </summary>
public interface IPowerMonitorSource
{
    /// <summary>True when a battery is present and a reading can be taken.</summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Machine-readable reason the monitor is unavailable (e.g. <c>no_battery</c>), or null when
    /// readings are available.
    /// </summary>
    string? UnavailableReason { get; }

    /// <summary>Reads the current system-wide power, or null when unavailable.</summary>
    PowerReading? Read();
}
