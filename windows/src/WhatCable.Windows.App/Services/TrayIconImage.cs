using Microsoft.UI.Xaml.Media.Imaging;

namespace WhatCable.Windows.App.Services;

/// <summary>Loads the tray icon image from the packaged assets.</summary>
internal static class TrayIconImage
{
    public static BitmapImage Load()
        => new(new Uri("ms-appx:///Assets/tray.ico"));
}
