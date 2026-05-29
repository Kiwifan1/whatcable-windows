using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WhatCable.Windows.App.Core.Localization;
using WhatCable.Windows.App.Core.ViewModels;

namespace WhatCable.Windows.App.Views;

/// <summary>
/// The flyout shown when the tray icon is clicked: a live status summary and the list of ports.
/// All commands are surfaced as events so <see cref="App"/> owns window/lifecycle decisions and
/// this control stays a thin, data-bound view over <see cref="TrayViewModel"/>.
/// </summary>
public sealed partial class TrayPopover : UserControl
{
    public TrayPopover()
    {
        InitializeComponent();
    }

    /// <summary>Applies localised labels to the popover's action buttons.</summary>
    public void ApplyLocalization(ILocalizer localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        RefreshButton.Content = localizer.Get(StringKeys.TrayRefresh);
        SettingsButton.Content = localizer.Get(StringKeys.TraySettings);
        QuitButton.Content = localizer.Get(StringKeys.TrayQuit);
        PowerMonitorLabel.Text = localizer.Get(StringKeys.ProPowerMonitor);
        PowerMonitorUpsell.Text = localizer.Get(StringKeys.ProUpsell);
    }

    /// <summary>The backing ViewModel. Set by <see cref="App"/> after construction.</summary>
    public TrayViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
            DataContext = value;
        }
    }

    private TrayViewModel? _viewModel;

    /// <summary>Raised when the user clicks a port row to open its detail window.</summary>
    public event EventHandler<PortViewModel>? PortInvoked;

    /// <summary>Raised when the user asks to open the Settings window.</summary>
    public event EventHandler? SettingsRequested;

    /// <summary>Raised when the user asks for an immediate refresh.</summary>
    public event EventHandler? RefreshRequested;

    /// <summary>Raised when the user asks to quit the app.</summary>
    public event EventHandler? QuitRequested;

    private void OnPortClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is PortViewModel port)
        {
            PortInvoked?.Invoke(this, port);
        }
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs e)
        => RefreshRequested?.Invoke(this, EventArgs.Empty);

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
        => SettingsRequested?.Invoke(this, EventArgs.Empty);

    private void OnQuitClicked(object sender, RoutedEventArgs e)
        => QuitRequested?.Invoke(this, EventArgs.Empty);
}
