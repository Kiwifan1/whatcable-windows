using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WhatCable.Core;
using WhatCable.Windows.App.Core.Localization;
using WhatCable.Windows.App.Core.Pro;
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
    private readonly IPowerMonitorSource? _powerMonitorSource;

    private Snapshot? _lastSnapshot;
    private bool _liquidPreviouslyDetected;

    public TrayViewModel(
        ISnapshotProvider snapshots,
        ISettingsStore settingsStore,
        ILocalizer localizer,
        INotificationService? notifications = null,
        IPowerMonitorSource? powerMonitorSource = null)
    {
        _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _notifications = notifications;
        _powerMonitorSource = powerMonitorSource;
        Ports = new ObservableCollection<PortViewModel>();

        var proUnlocked = ProEntitlement.IsUnlocked(_settingsStore.Load());
        PowerMonitor = new PowerMonitorViewModel(powerMonitorSource, proUnlocked);
    }

    public ObservableCollection<PortViewModel> Ports { get; }

    /// <summary>The Pro Power Monitor graph ViewModel (system-wide watts).</summary>
    public PowerMonitorViewModel PowerMonitor { get; private set; }

    [ObservableProperty]
    private string _statusSummary = string.Empty;

    [ObservableProperty]
    private bool _hasPorts;

    /// <summary>Localised liquid-detection banner text (Pro), or null when nothing to show.</summary>
    [ObservableProperty]
    private string? _liquidDetectionBanner;

    /// <summary>True when liquid is currently detected and the user has Pro.</summary>
    [ObservableProperty]
    private bool _isLiquidDetected;

    /// <summary>True when liquid detection is a Pro upsell (hardware supports it, Pro is locked).</summary>
    [ObservableProperty]
    private bool _showLiquidDetectionUpsell;

    /// <summary>Raised after every refresh for each detected connect/disconnect event.</summary>
    public event EventHandler<PortChange>? PortChanged;

    [RelayCommand]
    public void Refresh()
    {
        var snapshot = _snapshots.GetSnapshot();
        var settings = _settingsStore.Load();
        var proUnlocked = ProEntitlement.IsUnlocked(settings);

        var visible = snapshot.Ports.AsEnumerable();
        if (settings.HideEmptyPorts)
        {
            visible = visible.Where(p => p.ConnectionActive);
        }

        Ports.Clear();
        foreach (var port in visible)
        {
            Ports.Add(new PortViewModel(port, _localizer, proUnlocked));
        }

        HasPorts = Ports.Count > 0;
        StatusSummary = BuildStatusSummary(snapshot);

        UpdateLiquidDetection(snapshot, settings, proUnlocked);
        UpdatePowerMonitor(proUnlocked);

        EmitChanges(snapshot, settings);
        _lastSnapshot = snapshot;
    }

    private void UpdateLiquidDetection(Snapshot snapshot, AppSettings settings, bool proUnlocked)
    {
        var state = LiquidDetectionState.FromSnapshot(snapshot);

        ShowLiquidDetectionUpsell = !proUnlocked && state.Supported;
        IsLiquidDetected = proUnlocked && state.Detected;
        LiquidDetectionBanner = IsLiquidDetected ? _localizer.Get(StringKeys.LiquidDetectionBanner) : null;

        // Toast once on the dry -> detected transition (Pro + notifications enabled).
        if (IsLiquidDetected && !_liquidPreviouslyDetected
            && settings is { NotificationsEnabled: true })
        {
            _notifications?.Notify(
                _localizer.Get(StringKeys.NotificationLiquidDetectedTitle),
                _localizer.Get(StringKeys.NotificationLiquidDetectedBody));
        }

        _liquidPreviouslyDetected = IsLiquidDetected;
    }

    private void UpdatePowerMonitor(bool proUnlocked)
    {
        // Rebuild the monitor when the Pro entitlement changes so its gating reflects the new state.
        if (PowerMonitor.IsProUnlocked != proUnlocked)
        {
            PowerMonitor = new PowerMonitorViewModel(_powerMonitorSource, proUnlocked);
            OnPropertyChanged(nameof(PowerMonitor));
        }

        PowerMonitor.Sample();
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
