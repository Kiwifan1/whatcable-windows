using WhatCable.Core;

namespace WhatCable.Windows.Backend.Power;

/// <summary>
/// Produces the battery-level banner and an <see cref="AdapterSnapshot"/> from a
/// <see cref="ISystemPowerSource"/>. On a non-UCSI host the precise PD charger profile is
/// unavailable, so only the system-level power summary is reported.
/// </summary>
public sealed class PowerAdapter
{
    private readonly ISystemPowerSource _source;

    public PowerAdapter(ISystemPowerSource source)
        => _source = source ?? throw new ArgumentNullException(nameof(source));

    public AdapterSnapshot GetAdapter()
    {
        var info = _source.GetPowerInfo();
        return new AdapterSnapshot
        {
            Source = info.AcOnline ? "AC" : "Battery",
            Description = BuildBanner(info),
            IsWireless = false,
        };
    }

    public string GetBatteryBanner() => BuildBanner(_source.GetPowerInfo());

    internal static string BuildBanner(SystemPowerInfo info)
    {
        if (!info.BatteryPresent)
        {
            return info.AcOnline
                ? "On AC power — no battery detected (desktop or AC-only system)."
                : "No battery detected.";
        }

        var level = info.BatteryPercent is int percent
            ? $"Battery {percent}%"
            : "Battery";

        if (info.AcOnline)
        {
            if (info.Charging)
            {
                return $"{level} — charging (AC connected).";
            }

            return info.BatteryPercent >= 100
                ? $"{level} — fully charged (AC connected)."
                : $"{level} — on AC, not charging.";
        }

        if (info.RemainingMinutes is int minutes && minutes > 0)
        {
            return $"{level} — on battery, ~{FormatDuration(minutes)} remaining.";
        }

        return $"{level} — on battery.";
    }

    private static string FormatDuration(int minutes)
    {
        if (minutes < 60)
        {
            return $"{minutes} min";
        }

        var hours = minutes / 60;
        var mins = minutes % 60;
        return mins == 0 ? $"{hours} h" : $"{hours} h {mins} min";
    }
}
