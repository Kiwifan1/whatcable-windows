using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WhatCable.Windows.App.Core.Services;
using WhatCable.Windows.App.Core.Settings;

namespace WhatCable.Windows.App.Core.ViewModels;

/// <summary>A selectable UI language. <see cref="Tag"/> is <c>null</c> for "follow the system".</summary>
public sealed record LocaleOption(string DisplayName, string? Tag);

/// <summary>
/// Backs the Settings window: launch-at-login, notifications, vendor SDK toggle, and locale
/// selection. No WinUI dependency, so the persistence logic is unit-testable.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _store;
    private readonly IStartupTaskService? _startupTask;

    /// <summary>Locales shipped as <c>.resw</c> resources, plus a "system default" sentinel.</summary>
    public static readonly IReadOnlyList<LocaleOption> AvailableLocales = new[]
    {
        new LocaleOption("System Default", null),
        new LocaleOption("English", "en"),
        new LocaleOption("Հայերեն", "hy"),
        new LocaleOption("Italiano", "it"),
        new LocaleOption("Polski", "pl"),
        new LocaleOption("简体中文", "zh-Hans"),
    };

    public SettingsViewModel(ISettingsStore store, IStartupTaskService? startupTask = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _startupTask = startupTask;

        var settings = _store.Load();
        _launchAtLogin = settings.LaunchAtLogin || (startupTask?.IsEnabled ?? false);
        _notificationsEnabled = settings.NotificationsEnabled;
        _notifyOnCableChanges = settings.NotifyOnCableChanges;
        _hideEmptyPorts = settings.HideEmptyPorts;
        _vendorSdkEnabled = settings.VendorSdkEnabled;
        _selectedLocale = AvailableLocales.FirstOrDefault(l => l.Tag == settings.Locale) ?? AvailableLocales[0];
    }

    public IReadOnlyList<LocaleOption> Locales => AvailableLocales;

    /// <summary>True when the OS lets the user toggle launch-at-login from within the app.</summary>
    public bool CanToggleLaunchAtLogin => _startupTask?.CanToggle ?? true;

    [ObservableProperty]
    private bool _launchAtLogin;

    [ObservableProperty]
    private bool _notificationsEnabled;

    [ObservableProperty]
    private bool _notifyOnCableChanges;

    [ObservableProperty]
    private bool _hideEmptyPorts;

    [ObservableProperty]
    private bool _vendorSdkEnabled;

    [ObservableProperty]
    private LocaleOption _selectedLocale;

    /// <summary>Reflects the user's intent to the OS startup task, then mirrors the effective state.</summary>
    [RelayCommand]
    public async Task ApplyLaunchAtLoginAsync(bool enabled)
    {
        if (_startupTask is null)
        {
            LaunchAtLogin = enabled;
            return;
        }

        LaunchAtLogin = await _startupTask.SetEnabledAsync(enabled);
    }

    /// <summary>Persists the current values to the backing store.</summary>
    [RelayCommand]
    public void Save()
    {
        _store.Save(new AppSettings
        {
            LaunchAtLogin = LaunchAtLogin,
            NotificationsEnabled = NotificationsEnabled,
            NotifyOnCableChanges = NotifyOnCableChanges,
            HideEmptyPorts = HideEmptyPorts,
            VendorSdkEnabled = VendorSdkEnabled,
            Locale = SelectedLocale.Tag,
        });
    }
}
