using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WhatCable.Windows.App.Core.Localization;
using WhatCable.Windows.App.Core.ViewModels;

namespace WhatCable.Windows.App.Views;

/// <summary>
/// Settings window over <see cref="SettingsViewModel"/>: launch-at-login, notifications, port
/// visibility, vendor-SDK probing, and UI language. Labels are pulled from the localiser at
/// construction; values round-trip through the ViewModel and are persisted on close.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly ILocalizer _localizer;

    public SettingsWindow(SettingsViewModel viewModel, ILocalizer localizer)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

        InitializeComponent();

        Title = _localizer.Get(StringKeys.SettingsTitle);
        Root.DataContext = _viewModel;
        ApplyLocalization();

        // Persist whatever the user changed when the window closes.
        Closed += (_, _) => _viewModel.Save();
    }

    private void ApplyLocalization()
    {
        TitleText.Text = _localizer.Get(StringKeys.SettingsTitle);

        BehaviorHeader.Text = _localizer.Get(StringKeys.SettingsBehavior);
        LaunchAtLoginToggle.Header = _localizer.Get(StringKeys.SettingsLaunchAtLogin);
        NotificationsToggle.Header = _localizer.Get(StringKeys.SettingsNotifications);
        NotifyChangesToggle.Header = _localizer.Get(StringKeys.SettingsNotifyOnCableChanges);
        HideEmptyToggle.Header = _localizer.Get(StringKeys.SettingsHideEmptyPorts);
        VendorSdkToggle.Header = _localizer.Get(StringKeys.SettingsVendorSdk);
        VendorSdkDetail.Text = _localizer.Get(StringKeys.SettingsVendorSdkDetail);

        LanguageHeader.Text = _localizer.Get(StringKeys.SettingsLanguage);
        DoneButton.Content = _localizer.Get(StringKeys.SettingsDone);
    }

    private async void OnLaunchAtLoginToggled(object sender, RoutedEventArgs e)
    {
        // Toggled also fires when IsOn is set programmatically (initial binding and the
        // reconciliation assignment below). Ignore those: only act when the user actually changed
        // the switch to a state that differs from the ViewModel.
        if (LaunchAtLoginToggle.IsOn == _viewModel.LaunchAtLogin)
        {
            return;
        }

        // Route through the ViewModel so the OS startup task is actually toggled and the effective
        // state (which the OS may override) is reflected back into the switch.
        await _viewModel.ApplyLaunchAtLoginCommand.ExecuteAsync(LaunchAtLoginToggle.IsOn);
        LaunchAtLoginToggle.IsOn = _viewModel.LaunchAtLogin;
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();

    private void OnDoneClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.Save();
        Close();
    }
}
