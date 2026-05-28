namespace WhatCable.Windows.Backend.Power;

/// <summary>
/// Platform-neutral snapshot of system power state, sourced on Windows from
/// <c>GetSystemPowerStatus</c> and <c>CallNtPowerInformation(SystemBatteryState)</c>.
/// </summary>
public sealed record SystemPowerInfo
{
    /// <summary>True when running on external (AC) power.</summary>
    public bool AcOnline { get; init; }

    /// <summary>True when at least one battery is present.</summary>
    public bool BatteryPresent { get; init; }

    /// <summary>True when a battery is actively charging.</summary>
    public bool Charging { get; init; }

    /// <summary>Battery charge as a percentage (0–100), or null when unknown.</summary>
    public int? BatteryPercent { get; init; }

    /// <summary>Estimated battery runtime remaining, in minutes, or null when unknown.</summary>
    public int? RemainingMinutes { get; init; }
}
