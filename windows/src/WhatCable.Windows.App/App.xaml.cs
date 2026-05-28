// WinUI 3 application entry point. Tray icon, popover, and Settings window are
// wired up in PR 8. This scaffold is intentionally minimal so the solution
// builds end-to-end from PR 1 onwards.
using Microsoft.UI.Xaml;

namespace WhatCable.Windows.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Tray icon + popover wired up in PR 8.
    }
}
