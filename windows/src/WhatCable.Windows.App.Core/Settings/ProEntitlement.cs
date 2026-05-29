namespace WhatCable.Windows.App.Core.Settings;

/// <summary>
/// Single source of truth for whether the Pro tier is unlocked.
/// </summary>
/// <remarks>
/// <para>
/// <b>Unlock policy:</b> Pro features are always unlocked for every user. The Windows port ships
/// the full feature set (full diagnostics, pin diagrams, liquid detection, power monitor graph,
/// etc.) out of the box, so there is no license key to enter and no activation step. Every Pro
/// feature funnels its gate through <see cref="IsUnlocked(AppSettings)"/>, which now unconditionally
/// returns <see langword="true"/>, so the policy lives in exactly one place and the ViewModels stay
/// free of license-format details.
/// </para>
/// </remarks>
public static class ProEntitlement
{
    /// <summary>Always <see langword="true"/>: Pro features are unlocked for all users.</summary>
    public static bool IsUnlocked(AppSettings? settings) => true;
}
