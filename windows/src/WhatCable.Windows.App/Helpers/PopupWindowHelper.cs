using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace WhatCable.Windows.App.Helpers;

/// <summary>
/// Win32 interop helpers for styling WinUI 3 windows as tray popups or compact dialogs.
/// </summary>
internal static class PopupWindowHelper
{
    private const int GWL_EXSTYLE = -20;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const int TrayMargin = 16;

    private const int SW_HIDE = 0;
    private const int SW_SHOWNOACTIVATE = 4;

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

    /// <summary>
    /// Applies tray-popup styling to <paramref name="window"/> and positions it above the tray.
    /// On focus loss the window is hidden (not closed) so the tray app stays alive.
    /// Call <see cref="ShowOrActivate"/> to bring a previously hidden popup back.
    /// </summary>
    public static void ApplyTrayPopupStyle(Window window, int width, int height)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // 1. Remove title bar and border, disable resize/minimize/maximize.
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        // 2. WS_EX_TOOLWINDOW: hide from taskbar and Alt+Tab.
        var exStyle = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)(exStyle | WS_EX_TOOLWINDOW));

        // 3. Windows 11 rounded corners.
        var preference = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));

        // 4. Size the window.
        appWindow.Resize(new global::Windows.Graphics.SizeInt32(width, height));

        // 5. Position at the bottom-right of the work area, flush against the taskbar.
        //    WorkArea already excludes the taskbar, so bottom of work area = top of taskbar.
        var area = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var workArea = area.WorkArea;
        appWindow.Move(new global::Windows.Graphics.PointInt32(
            workArea.X + workArea.Width - width - TrayMargin,
            workArea.Y + workArea.Height - height));

        // 6. Auto-hide on focus loss (hide, not close — prevents WinUI app exit).
        var hasBeenActivated = false;
        window.Activated += (_, args) =>
        {
            if (args.WindowActivationState != WindowActivationState.Deactivated)
            {
                hasBeenActivated = true;
            }
            else if (hasBeenActivated)
            {
                ShowWindow(hwnd, SW_HIDE);
            }
        };
    }

    /// <summary>
    /// Shows a popup window that was previously hidden by the auto-dismiss handler,
    /// or activates it if already visible.
    /// </summary>
    public static void ShowOrActivate(Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        ShowWindow(hwnd, SW_SHOWNOACTIVATE);
        SetForegroundWindow(hwnd);
        window.Activate();
    }

    /// <summary>
    /// Sizes and centers a regular window (Settings, Port Detail) on the primary display.
    /// Keeps the title bar and taskbar entry — this is for normal windows, not tray popups.
    /// </summary>
    public static void ApplyCompactWindowStyle(Window window, int width, int height)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new global::Windows.Graphics.SizeInt32(width, height));

        // Center on the primary display work area.
        var area = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var workArea = area.WorkArea;
        appWindow.Move(new global::Windows.Graphics.PointInt32(
            workArea.X + (workArea.Width - width) / 2,
            workArea.Y + (workArea.Height - height) / 2));
    }
}
