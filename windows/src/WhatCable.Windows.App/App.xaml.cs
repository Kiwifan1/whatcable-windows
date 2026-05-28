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

    private TrayViewModel? _trayViewModel;
    private TaskbarIcon? _trayIcon;
    private DispatcherQueueTimer? _pollTimer;
    private SettingsWindow? _settingsWindow;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        ApplyLanguageOverride(_settingsStore.Load());
        AppNotificationManager.Default.Register();

        _trayViewModel = new TrayViewModel(
            new BackendSnapshotProvider(),
            _settingsStore,
            _localizer,
            new ToastNotificationService());

        CreateTrayIcon();
        StartPolling();
    }

    private void CreateTrayIcon()
    {
        var popover = new TrayPopover { ViewModel = _trayViewModel! };
        popover.ApplyLocalization(_localizer);
        popover.PortInvoked += (_, port) => ShowPortDetail(port);
        popover.SettingsRequested += (_, _) => ShowSettings();
        popover.RefreshRequested += (_, _) => _trayViewModel!.Refresh();
        popover.QuitRequested += (_, _) => Quit();

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = _localizer.Get(StringKeys.TrayTooltip),
            IconSource = TrayIconImage.Load(),
            NoLeftClickDelay = true,
            ContextFlyout = BuildContextMenu(),
            TrayPopup = popover,
        };
        _trayIcon.ForceCreate();
    }

    private MenuFlyout BuildContextMenu()
    {
        var menu = new MenuFlyout();

        var refresh = new MenuFlyoutItem { Text = _localizer.Get(StringKeys.TrayRefresh) };
        refresh.Click += (_, _) => _trayViewModel!.Refresh();

        var settings = new MenuFlyoutItem { Text = _localizer.Get(StringKeys.TraySettings) };
        settings.Click += (_, _) => ShowSettings();

        var quit = new MenuFlyoutItem { Text = _localizer.Get(StringKeys.TrayQuit) };
        quit.Click += (_, _) => Quit();

        menu.Items.Add(refresh);
        menu.Items.Add(settings);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(quit);
        return menu;
    }

    private void StartPolling()
    {
        var queue = DispatcherQueue.GetForCurrentThread();
        _pollTimer = queue.CreateTimer();
        _pollTimer.Interval = PollInterval;
        _pollTimer.Tick += (_, _) => _trayViewModel!.Refresh();
        _pollTimer.Start();
        _trayViewModel!.Refresh();
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
        _trayIcon?.Dispose();
        AppNotificationManager.Default.Unregister();
        Exit();
    }

    private static void ApplyLanguageOverride(AppSettings settings)
    {
        Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride =
            settings.Locale ?? string.Empty;
    }
}
