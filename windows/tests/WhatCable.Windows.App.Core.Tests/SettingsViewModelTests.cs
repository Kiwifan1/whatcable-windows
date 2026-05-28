using WhatCable.Windows.App.Core.Settings;
using WhatCable.Windows.App.Core.ViewModels;
using Xunit;

namespace WhatCable.Windows.App.Core.Tests;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void Constructor_LoadsExistingSettings()
    {
        var store = new InMemorySettingsStore(new AppSettings
        {
            NotificationsEnabled = false,
            HideEmptyPorts = true,
            VendorSdkEnabled = true,
            Locale = "it",
        });

        var vm = new SettingsViewModel(store);

        Assert.False(vm.NotificationsEnabled);
        Assert.True(vm.HideEmptyPorts);
        Assert.True(vm.VendorSdkEnabled);
        Assert.Equal("it", vm.SelectedLocale.Tag);
    }

    [Fact]
    public void SelectedLocale_DefaultsToSystemDefaultWhenUnset()
    {
        var vm = new SettingsViewModel(new InMemorySettingsStore());

        Assert.Null(vm.SelectedLocale.Tag);
        Assert.Same(SettingsViewModel.AvailableLocales[0], vm.SelectedLocale);
    }

    [Fact]
    public void AvailableLocales_CoverTheFiveShippedLanguages()
    {
        var tags = SettingsViewModel.AvailableLocales.Select(l => l.Tag).ToArray();

        Assert.Contains(null, tags); // system default
        foreach (var expected in new[] { "en", "hy", "it", "pl", "zh-Hans" })
        {
            Assert.Contains(expected, tags);
        }
    }

    [Fact]
    public void Save_PersistsAllValues()
    {
        var store = new InMemorySettingsStore();
        var vm = new SettingsViewModel(store)
        {
            NotificationsEnabled = true,
            NotifyOnCableChanges = false,
            HideEmptyPorts = true,
            VendorSdkEnabled = true,
            SelectedLocale = SettingsViewModel.AvailableLocales.First(l => l.Tag == "pl"),
            LicenseKey = LicenseValidator.Create("ABCDE", "FGHJK", "MNPQ"),
        };

        vm.Save();

        var saved = store.Load();
        Assert.True(saved.NotificationsEnabled);
        Assert.False(saved.NotifyOnCableChanges);
        Assert.True(saved.HideEmptyPorts);
        Assert.True(saved.VendorSdkEnabled);
        Assert.Equal("pl", saved.Locale);
        Assert.NotNull(saved.ProLicenseKey);
        Assert.True(LicenseValidator.IsValid(saved.ProLicenseKey));
    }

    [Fact]
    public void Save_BlankLicenseKeyPersistsAsNull()
    {
        var store = new InMemorySettingsStore();
        var vm = new SettingsViewModel(store) { LicenseKey = "   " };

        vm.Save();

        Assert.Null(store.Load().ProLicenseKey);
    }

    [Fact]
    public void IsProLicensed_TracksLicenseKey()
    {
        var vm = new SettingsViewModel(new InMemorySettingsStore());
        Assert.False(vm.IsProLicensed);

        vm.LicenseKey = LicenseValidator.Create("ABCDE", "FGHJK", "MNPQ");
        Assert.True(vm.IsProLicensed);
    }

    [Fact]
    public async Task ApplyLaunchAtLogin_ReflectsOsEffectiveState()
    {
        var startup = new FakeStartupTaskService(isEnabled: false) { ForcedResult = false };
        var vm = new SettingsViewModel(new InMemorySettingsStore(), startup);

        await vm.ApplyLaunchAtLoginAsync(true);

        // OS denied the request, so the VM mirrors the real (still-disabled) state.
        Assert.False(vm.LaunchAtLogin);
    }

    [Fact]
    public async Task ApplyLaunchAtLogin_EnablesWhenOsAllows()
    {
        var startup = new FakeStartupTaskService(isEnabled: false);
        var vm = new SettingsViewModel(new InMemorySettingsStore(), startup);

        await vm.ApplyLaunchAtLoginAsync(true);

        Assert.True(vm.LaunchAtLogin);
        Assert.True(startup.IsEnabled);
    }

    [Fact]
    public void CanToggleLaunchAtLogin_ReflectsStartupService()
    {
        var vm = new SettingsViewModel(new InMemorySettingsStore(), new FakeStartupTaskService(canToggle: false));

        Assert.False(vm.CanToggleLaunchAtLogin);
    }
}
