using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using WhatCable.Windows.App.Core.Localization;
using WhatCable.Windows.App.Core.Services;
using WhatCable.Windows.App.Core.Settings;
using WhatCable.Windows.App.Core.ViewModels;
using WhatCable.Windows.App.Services;
using WhatCable.Windows.App.Views;
using WhatCable.Windows.Backend;

namespace WhatCable.Windows.App;

/// <summary>
/// Tray-only WinUI 3 application: there is no main window. On launch it wires up the platform
/// services, builds the <see cref="TrayViewModel"/>, creates the tray icon + popover, and polls
/// the backend on a timer so the summary and notifications stay live.
/// </summary>
public partial class App : Application
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly ISettingsStore _settingsStore = new JsonFileSettingsStore();
    private readonly ILocalizer _localizer = new ResourceLocalizer();
    private readonly IStartupTaskService _startupTask = new StartupTaskService();
    private readonly WidgetPublisher _widgetPublisher = new();

    // Caches the latest snapshot so the tray VM and widget publisher share one backend query.
    private readonly CachingSnapshotProvider _snapshotProvider = new();

    private TrayViewModel? _trayViewModel;
    private TaskbarIcon? _trayIcon;
    private TrayPopover? _popover;
    private Window? _popoverWindow;
    private MenuFlyoutItem? _refreshMenuItem;
    private MenuFlyoutItem? _settingsMenuItem;
    private MenuFlyoutItem? _quitMenuItem;
    private DispatcherQueue? _dispatcherQueue;
    private DispatcherQueueTimer? _pollTimer;
    private SettingsWindow? _settingsWindow;

    // Cancels any in-flight background poll work when a newer poll fires.
    private CancellationTokenSource _publishCts = new();

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        ApplyLanguageOverride(_settingsStore.Load());
        AppNotificationManager.Default.Register();

        _trayViewModel = new TrayViewModel(
            _snapshotProvider,
            _settingsStore,
            _localizer,
            new ToastNotificationService(),
            new PowerMonitorSource());

        CreateTrayIcon();
        StartPolling();
    }

    private void CreateTrayIcon()
    {
        _popover = new TrayPopover { ViewModel = _trayViewModel! };
        _popover.ApplyLocalization(_localizer);
        _popover.PortInvoked += (_, port) => ShowPortDetail(port);
        _popover.SettingsRequested += (_, _) => ShowSettings();
        _popover.RefreshRequested += (_, _) => _trayViewModel!.Refresh();
        _popover.QuitRequested += (_, _) => Quit();

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = _localizer.Get(StringKeys.TrayTooltip),
            IconSource = TrayIconImage.Load(),
            NoLeftClickDelay = true,
            ContextFlyout = BuildContextMenu(),
            MenuActivation = H.NotifyIcon.Core.PopupActivationMode.RightClick,
            LeftClickCommand = new RelayCommand(ShowPopover),
        };
        _trayIcon.ForceCreate();
    }

    private void ShowPopover()
    {
        if (_popoverWindow is not null)
        {
            _popoverWindow.Activate();
            return;
        }

        _popoverWindow = new Window
        {
            Content = _popover,
            SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop(),
        };
        _popoverWindow.Closed += (_, _) => _popoverWindow = null;

        Helpers.PopupWindowHelper.ApplyTrayPopupStyle(_popoverWindow, width: 400, height: 500);

        _trayViewModel!.Refresh();
        _popoverWindow.Activate();
    }

    private MenuFlyout BuildContextMenu()
    {
        var menu = new MenuFlyout();

        _refreshMenuItem = new MenuFlyoutItem { Text = _localizer.Get(StringKeys.TrayRefresh) };
        _refreshMenuItem.Click += (_, _) => _trayViewModel!.Refresh();

        _settingsMenuItem = new MenuFlyoutItem { Text = _localizer.Get(StringKeys.TraySettings) };
        _settingsMenuItem.Click += (_, _) => ShowSettings();

        _quitMenuItem = new MenuFlyoutItem { Text = _localizer.Get(StringKeys.TrayQuit) };
        _quitMenuItem.Click += (_, _) => Quit();

        menu.Items.Add(_refreshMenuItem);
        menu.Items.Add(_settingsMenuItem);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(_quitMenuItem);
        return menu;
    }

    /// <summary>Re-applies the current language to the tray UI built once at startup.</summary>
    private void ApplyTrayLocalization()
    {
        _popover?.ApplyLocalization(_localizer);

        if (_trayIcon is not null)
        {
            _trayIcon.ToolTipText = _localizer.Get(StringKeys.TrayTooltip);
        }

        if (_refreshMenuItem is not null)
        {
            _refreshMenuItem.Text = _localizer.Get(StringKeys.TrayRefresh);
        }

        if (_settingsMenuItem is not null)
        {
            _settingsMenuItem.Text = _localizer.Get(StringKeys.TraySettings);
        }

        if (_quitMenuItem is not null)
        {
            _quitMenuItem.Text = _localizer.Get(StringKeys.TrayQuit);
        }
    }

    private void StartPolling()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _pollTimer = _dispatcherQueue.CreateTimer();
        _pollTimer.Interval = PollInterval;
        _pollTimer.Tick += (_, _) => Poll();
        _pollTimer.Start();
        Poll();
    }

    /// <summary>
    /// Builds a backend report on the thread pool (once per tick), updates the snapshot cache so
    /// <see cref="TrayViewModel"/> sees fresh data, then dispatches the UI refresh back to the
    /// dispatcher thread and publishes the same report to the widget provider. Any in-flight work
    /// from a previous tick is cancelled before the new one starts.
    /// </summary>
    private void Poll()
    {
        // Cancel any in-flight widget publish from a previous poll and start fresh.
        _publishCts.Cancel();
        _publishCts.Dispose();
        _publishCts = new CancellationTokenSource();
        var cts = _publishCts;
        var dq = _dispatcherQueue!;

        _ = Task.Run(async () =>
        {
            try
            {
                // Build the report once; both the tray VM and the widget publisher use it.
                var report = SnapshotBuilder.CreateDefault().BuildReport();
                if (cts.IsCancellationRequested) return;

                // Update the cache so the tray ViewModel picks up the new snapshot.
                _snapshotProvider.Update(report.Snapshot);

                // Refresh the tray UI on the dispatcher thread.
                dq.TryEnqueue(() => _trayViewModel!.Refresh());

                // Publish to the widget provider; interruptible if the next poll fires first.
                await _widgetPublisher.PublishAsync(report, cts.Token).ConfigureAwait(false);
            }
            catch
            {
                // Best-effort; widget publishing must never disrupt the tray app.
            }
        });
    }

    private void ShowSettings()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(new SettingsViewModel(_settingsStore, _startupTask), _localizer);
            _settingsWindow.Closed += (_, _) =>
            {
                _settingsWindow = null;
                // Re-apply language and refresh so changes take effect immediately.
                ApplyLanguageOverride(_settingsStore.Load());
                ApplyTrayLocalization();
                _trayViewModel!.Refresh();
            };
        }

        _settingsWindow.Activate();
    }

    private void ShowPortDetail(PortViewModel port)
    {
        var window = new PortDetailWindow(port, _localizer);
        window.Activate();
    }

    private void Quit()
    {
        _pollTimer?.Stop();
        _publishCts.Cancel();
        _trayIcon?.Dispose();
        AppNotificationManager.Default.Unregister();
        Exit();
    }

    private static void ApplyLanguageOverride(AppSettings settings)
    {
        // PrimaryLanguageOverride throws E_INVALIDARG for empty strings; only set when a real
        // BCP-47 tag is configured. Null/empty means "follow the system language".
        if (string.IsNullOrWhiteSpace(settings.Locale))
        {
            return;
        }

        try
        {
            Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = settings.Locale;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Invalid tag or unsupported context — fall back to system language.
        }
    }
}
