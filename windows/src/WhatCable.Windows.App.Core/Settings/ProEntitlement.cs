namespace WhatCable.Windows.App.Core.Settings;

/// <summary>
/// Single source of truth for whether the Pro tier is unlocked.
/// </summary>
/// <remarks>
/// <para>
/// <b>License-gate decision (PR 10):</b> the Windows port uses an <i>offline key</i> rather than
/// porting the macOS App Store receipt-verification scheme. The macOS receipt check relies on a
/// StoreKit receipt signed for an App Store distribution; the Windows app ships as an unsigned,
/// non-Store (Inno Setup) build in v1, so there is no equivalent receipt to verify. An offline key
/// (validated structurally by <see cref="LicenseValidator"/>) works for both the packaged MSIX and
/// the unpackaged installer, and keeps the app fully functional without any network round-trip.
/// </para>
/// <para>
/// Every Pro feature funnels its gate through <see cref="IsUnlocked(AppSettings)"/> so the policy
/// lives in exactly one place and the ViewModels stay free of license-format details.
/// </para>
/// </remarks>
public static class ProEntitlement
{
    /// <summary>True when the stored license key is a structurally valid Pro key.</summary>
    public static bool IsUnlocked(AppSettings? settings)
        => settings is not null && LicenseValidator.IsValid(settings.ProLicenseKey);
}
