using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WhatCable.Core;
using WhatCable.Windows.App.Core.Localization;
using WhatCable.Windows.App.Core.Services;
using WhatCable.Windows.App.Core.Settings;

namespace WhatCable.Windows.App.Core.ViewModels;

/// <summary>
/// Backs the tray icon and its popover. Pulls a <see cref="Snapshot"/> on demand, projects it into
/// per-port ViewModels (honouring the "hide empty ports" setting), and raises connect/disconnect
/// notifications. Contains no WinUI types so it can be exercised without a UI thread.
/// </summary>
public sealed partial class TrayViewModel : ObservableObject
{
    private readonly ISnapshotProvider _snapshots;
    private readonly ISettingsStore _settingsStore;
    private readonly ILocalizer _localizer;
    private readonly INotificationService? _notifications;

    private Snapshot? _lastSnapshot;

    public TrayViewModel(
        ISnapshotProvider snapshots,
        ISettingsStore settingsStore,
        ILocalizer localizer,
        INotificationService? notifications = null)
    {
        _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _notifications = notifications;
        Ports = new ObservableCollection<PortViewModel>();
    }

    public ObservableCollection<PortViewModel> Ports { get; }

    [ObservableProperty]
    private string _statusSummary = string.Empty;

    [ObservableProperty]
    private bool _hasPorts;

    /// <summary>Raised after every refresh for each detected connect/disconnect event.</summary>
    public event EventHandler<PortChange>? PortChanged;

    [RelayCommand]
    public void Refresh()
    {
        var snapshot = _snapshots.GetSnapshot();
        var settings = _settingsStore.Load();

        var visible = snapshot.Ports.AsEnumerable();
        if (settings.HideEmptyPorts)
        {
            visible = visible.Where(p => p.ConnectionActive);
        }

        Ports.Clear();
        foreach (var port in visible)
        {
            Ports.Add(new PortViewModel(port, _localizer));
        }

        HasPorts = Ports.Count > 0;
        StatusSummary = BuildStatusSummary(snapshot);

        EmitChanges(snapshot, settings);
        _lastSnapshot = snapshot;
    }

    private string BuildStatusSummary(Snapshot snapshot)
    {
        if (snapshot.Ports.Count == 0)
        {
            return _localizer.Get(StringKeys.TrayNoPortsDetected);
        }

        var active = snapshot.Ports.Count(p => p.ConnectionActive);
        return active == 0
            ? _localizer.Get(StringKeys.TrayNothingConnected)
            : _localizer.Format(StringKeys.TrayPortsSummary, active, snapshot.Ports.Count);
    }

    private void EmitChanges(Snapshot snapshot, AppSettings settings)
    {
        var changes = SnapshotChangeDetector.Detect(_lastSnapshot, snapshot);
        foreach (var change in changes)
        {
            PortChanged?.Invoke(this, change);

            if (settings is { NotificationsEnabled: true, NotifyOnCableChanges: true })
            {
                _notifications?.Notify(TitleFor(change), MessageFor(change));
            }
        }
    }

    private string TitleFor(PortChange change) => change.Kind switch
    {
        PortChangeKind.ChargerConnected => _localizer.Get(StringKeys.NotificationChargerConnected),
        PortChangeKind.ChargerDisconnected => _localizer.Get(StringKeys.NotificationChargerDisconnected),
        PortChangeKind.DeviceConnected => _localizer.Get(StringKeys.NotificationDeviceConnected),
        PortChangeKind.DeviceDisconnected => _localizer.Get(StringKeys.NotificationDeviceDisconnected),
        _ => _localizer.Get(StringKeys.CommonUnknown),
    };

    private static string MessageFor(PortChange change)
        => string.IsNullOrEmpty(change.Detail) ? change.PortName : $"{change.PortName} — {change.Detail}";
}
