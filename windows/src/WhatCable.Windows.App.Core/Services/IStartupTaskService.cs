namespace WhatCable.Windows.App.Core.Services;

/// <summary>
/// Controls the packaged <c>StartupTask</c> used for launch-at-login. The WinUI app implements this
/// over <c>Windows.ApplicationModel.StartupTask</c>; tests use a fake.
/// </summary>
public interface IStartupTaskService
{
    /// <summary>Whether the app is currently set to launch at login.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Whether the user can still toggle the setting from within the app. Windows lets the user
    /// disable a startup task from Task Manager / Settings, after which the app can no longer
    /// re-enable it programmatically.
    /// </summary>
    bool CanToggle { get; }

    /// <summary>
    /// Requests the new launch-at-login state. Returns the effective state afterwards (which may
    /// differ from <paramref name="enabled"/> if the OS denied the request).
    /// </summary>
    Task<bool> SetEnabledAsync(bool enabled);
}
