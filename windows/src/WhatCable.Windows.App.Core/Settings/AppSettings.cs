namespace WhatCable.Windows.App.Core.Settings;

/// <summary>
/// User-facing preferences surfaced in the Settings window. Persisted via
/// <see cref="ISettingsStore"/> (ApplicationData LocalSettings in the packaged app).
/// </summary>
public sealed record AppSettings
{
    /// <summary>Start WhatCable when the user signs in (backed by a packaged <c>StartupTask</c>).</summary>
    public bool LaunchAtLogin { get; init; }

    /// <summary>Master switch for toast notifications.</summary>
    public bool NotificationsEnabled { get; init; } = true;

    /// <summary>Raise a toast when a cable/charger/device connects or disconnects.</summary>
    public bool NotifyOnCableChanges { get; init; } = true;

    /// <summary>Hide ports with nothing plugged in from the popover summary.</summary>
    public bool HideEmptyPorts { get; init; }

    /// <summary>Allow optional vendor GPU SDK probes (NVAPI / AMD ADL / Intel IGCL).</summary>
    public bool VendorSdkEnabled { get; init; }

    /// <summary>BCP-47 locale override, or <c>null</c> to follow the system language.</summary>
    public string? Locale { get; init; }

    /// <summary>Stored Pro license key, or <c>null</c> when unlicensed.</summary>
    public string? ProLicenseKey { get; init; }
}
