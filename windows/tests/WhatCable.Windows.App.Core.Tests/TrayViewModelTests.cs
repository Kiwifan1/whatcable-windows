using WhatCable.Core;
using WhatCable.Windows.App.Core.Localization;
using WhatCable.Windows.App.Core.Services;
using WhatCable.Windows.App.Core.Settings;
using WhatCable.Windows.App.Core.ViewModels;
using Xunit;
using static WhatCable.Windows.App.Core.Tests.SnapshotFactory;

namespace WhatCable.Windows.App.Core.Tests;

public sealed class TrayViewModelTests
{
    private static ILocalizer Localizer() => new DictionaryLocalizer(new Dictionary<string, string>
    {
        [StringKeys.TrayNothingConnected] = "Nothing connected",
        [StringKeys.TrayNoPortsDetected] = "No USB-C ports detected",
        [StringKeys.TrayPortsSummary] = "{0} of {1} ports active",
        [StringKeys.NotificationDeviceConnected] = "Device connected",
        [StringKeys.NotificationDeviceDisconnected] = "USB device disconnected",
        [StringKeys.NotificationChargerConnected] = "Charger connected",
        [StringKeys.NotificationChargerDisconnected] = "Charger disconnected",
    });

    [Fact]
    public void Refresh_PopulatesPortsAndSummary()
    {
        var snapshot = Snapshot(null,
            Port("Port 1", active: true, Device("Keyboard")),
            Port("Port 2", active: false));
        var vm = new TrayViewModel(new FakeSnapshotProvider(snapshot), new InMemorySettingsStore(), Localizer());

        vm.Refresh();

        Assert.Equal(2, vm.Ports.Count);
        Assert.True(vm.HasPorts);
        Assert.Equal("1 of 2 ports active", vm.StatusSummary);
    }

    [Fact]
    public void Refresh_HideEmptyPorts_FiltersInactivePorts()
    {
        var snapshot = Snapshot(null,
            Port("Port 1", active: true, Device("Keyboard")),
            Port("Port 2", active: false));
        var store = new InMemorySettingsStore(new AppSettings { HideEmptyPorts = true });
        var vm = new TrayViewModel(new FakeSnapshotProvider(snapshot), store, Localizer());

        vm.Refresh();

        Assert.Single(vm.Ports);
        Assert.Equal("Port 1", vm.Ports[0].Name);
    }

    [Fact]
    public void Refresh_NoPorts_ShowsNoPortsDetected()
    {
        var vm = new TrayViewModel(new FakeSnapshotProvider(Snapshot(null)), new InMemorySettingsStore(), Localizer());

        vm.Refresh();

        Assert.False(vm.HasPorts);
        Assert.Equal("No USB-C ports detected", vm.StatusSummary);
    }

    [Fact]
    public void Refresh_NothingActive_ShowsNothingConnected()
    {
        var snapshot = Snapshot(null, Port("Port 1", active: false));
        var vm = new TrayViewModel(new FakeSnapshotProvider(snapshot), new InMemorySettingsStore(), Localizer());

        vm.Refresh();

        Assert.Equal("Nothing connected", vm.StatusSummary);
    }

    [Fact]
    public void Refresh_RaisesNotificationsOnSecondRefreshWhenDeviceConnects()
    {
        var first = Snapshot(null, Port("Port 1", active: false));
        var second = Snapshot(null, Port("Port 1", active: true, Device("Keyboard")));
        var notifier = new RecordingNotificationService();
        var vm = new TrayViewModel(
            new FakeSnapshotProvider(first, second),
            new InMemorySettingsStore(),
            Localizer(),
            notifier);

        vm.Refresh(); // baseline — no previous snapshot, no toast
        Assert.Empty(notifier.Notifications);

        vm.Refresh(); // device connected
        var toast = Assert.Single(notifier.Notifications);
        Assert.Equal("Device connected", toast.Title);
    }

    [Fact]
    public void Refresh_RespectsNotificationSettings()
    {
        var first = Snapshot(null, Port("Port 1", active: false));
        var second = Snapshot(null, Port("Port 1", active: true, Device("Keyboard")));
        var notifier = new RecordingNotificationService();
        var store = new InMemorySettingsStore(new AppSettings { NotificationsEnabled = false });
        var vm = new TrayViewModel(new FakeSnapshotProvider(first, second), store, Localizer(), notifier);

        vm.Refresh();
        vm.Refresh();

        Assert.Empty(notifier.Notifications);
    }

    [Fact]
    public void Refresh_RaisesPortChangedEvent()
    {
        var first = Snapshot(null, Port("Port 1", active: false));
        var second = Snapshot(null, Port("Port 1", active: true, Device("Keyboard")));
        var vm = new TrayViewModel(new FakeSnapshotProvider(first, second), new InMemorySettingsStore(), Localizer());
        var events = new List<PortChange>();
        vm.PortChanged += (_, e) => events.Add(e);

        vm.Refresh();
        vm.Refresh();

        var change = Assert.Single(events);
        Assert.Equal(PortChangeKind.DeviceConnected, change.Kind);
    }
}
