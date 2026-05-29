using WhatCable.Windows.App.Core.Localization;
using WhatCable.Windows.App.Core.Services;
using WhatCable.Windows.App.Core.Settings;
using WhatCable.Windows.App.Core.ViewModels;
using Xunit;
using static WhatCable.Windows.App.Core.Tests.SnapshotFactory;

namespace WhatCable.Windows.App.Core.Tests;

public sealed class TrayViewModelProTests
{
    private static ILocalizer Localizer() => new DictionaryLocalizer(new Dictionary<string, string>
    {
        [StringKeys.TrayNothingConnected] = "Nothing connected",
        [StringKeys.TrayNoPortsDetected] = "No USB-C ports detected",
        [StringKeys.TrayPortsSummary] = "{0} of {1} ports active",
        [StringKeys.LiquidDetectionBanner] = "Liquid detected on a USB-C port",
        [StringKeys.NotificationLiquidDetectedTitle] = "Liquid detected",
        [StringKeys.NotificationLiquidDetectedBody] = "Disconnect the cable",
    });

    private static InMemorySettingsStore ProStore(bool hideEmpty = false)
        => new(new AppSettings
        {
            HideEmptyPorts = hideEmpty,
            NotificationsEnabled = true,
            ProLicenseKey = LicenseValidator.Create("ABCDE", "FGHJK", "MNPQ"),
        });

    [Fact]
    public void LiquidDetection_NoLicenseKey_StillShowsBanner()
    {
        var snapshot = Snapshot(null,
            UcsiJson.Port("Port 1", true, UcsiJson.Build(liquidDetectionSupported: true, liquidDetected: true)));
        var settings = new InMemorySettingsStore(new AppSettings { NotificationsEnabled = true });
        var notifications = new RecordingNotificationService();
        var vm = new TrayViewModel(new FakeSnapshotProvider(snapshot), settings, Localizer(), notifications);

        vm.Refresh();

        Assert.False(vm.ShowLiquidDetectionUpsell);
        Assert.True(vm.IsLiquidDetected);
        Assert.Equal("Liquid detected on a USB-C port", vm.LiquidDetectionBanner);
    }

    [Fact]
    public void LiquidDetection_ProUnlocked_ShowsBannerAndToasts()
    {
        var snapshot = Snapshot(null,
            UcsiJson.Port("Port 1", true, UcsiJson.Build(liquidDetectionSupported: true, liquidDetected: true)));
        var notifications = new RecordingNotificationService();
        var vm = new TrayViewModel(new FakeSnapshotProvider(snapshot), ProStore(), Localizer(), notifications);

        vm.Refresh();

        Assert.True(vm.IsLiquidDetected);
        Assert.Equal("Liquid detected on a USB-C port", vm.LiquidDetectionBanner);
        Assert.Single(notifications.Notifications);
        Assert.Equal("Liquid detected", notifications.Notifications[0].Title);
    }

    [Fact]
    public void LiquidDetection_ToastsOncePerTransition()
    {
        var dry = Snapshot(null,
            UcsiJson.Port("Port 1", true, UcsiJson.Build(liquidDetectionSupported: true, liquidDetected: false)));
        var wet = Snapshot(null,
            UcsiJson.Port("Port 1", true, UcsiJson.Build(liquidDetectionSupported: true, liquidDetected: true)));
        var notifications = new RecordingNotificationService();
        var vm = new TrayViewModel(new FakeSnapshotProvider(dry, wet, wet), ProStore(), Localizer(), notifications);

        vm.Refresh(); // dry
        vm.Refresh(); // wet -> toast
        vm.Refresh(); // still wet -> no new toast

        Assert.Single(notifications.Notifications);
    }

    [Fact]
    public void PowerMonitor_NoLicenseKey_StillSamples()
    {
        var snapshot = Snapshot(null, Port("Port 1", active: true));
        var source = new FakePowerMonitorSource(isAvailable: true, readings: new PowerReading?[] { new() { Watts = 10 } });
        var vm = new TrayViewModel(new FakeSnapshotProvider(snapshot), new InMemorySettingsStore(), Localizer(), null, source);

        vm.Refresh();

        Assert.True(vm.PowerMonitor.IsProUnlocked);
        Assert.False(vm.PowerMonitor.ShowUpsell);
        Assert.True(vm.PowerMonitor.HasData);
    }

    [Fact]
    public void PowerMonitor_ProUnlocked_SamplesOnRefresh()
    {
        var snapshot = Snapshot(null, Port("Port 1", active: true));
        var source = new FakePowerMonitorSource(isAvailable: true, readings: new PowerReading?[] { new() { Watts = 10 } });
        var vm = new TrayViewModel(new FakeSnapshotProvider(snapshot), ProStore(), Localizer(), null, source);

        vm.Refresh();

        Assert.True(vm.PowerMonitor.IsProUnlocked);
        Assert.True(vm.PowerMonitor.HasData);
        Assert.Equal(10, vm.PowerMonitor.CurrentWatts);
    }
}
