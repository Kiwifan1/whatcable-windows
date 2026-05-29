using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace WhatCable.Windows.App.Helpers;

/// <summary>
/// Win32 interop helpers for styling WinUI 3 windows as tray popups or connected panel views.
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
    /// Applies tray-popup styling and auto-hides on focus loss.
    /// Call <see cref="ShowOrActivate"/> to bring a hidden popup back.
    /// </summary>
    public static void ApplyTrayPopupStyle(Window window, int width, int height)
    {
        var hwnd = ApplyToolWindowChrome(window, width, height);

        // Auto-hide on focus loss (hide, not close — prevents WinUI app exit).
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
    /// Applies the same tray-panel chrome (borderless, toolwindow, rounded corners, positioned
    /// bottom-right) but does NOT auto-hide on focus loss. The user closes these panels via an
    /// explicit close button.
    /// </summary>
    public static void ApplyTrayPanelStyle(Window window, int width, int height)
    {
        ApplyToolWindowChrome(window, width, height);
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
    /// Shared chrome for all tray-associated windows: borderless, no taskbar entry,
    /// DWM rounded corners, sized and positioned at the bottom-right of the work area.
    /// </summary>
    private static IntPtr ApplyToolWindowChrome(Window window, int width, int height)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // Remove title bar and border, disable resize/minimize/maximize.
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        // WS_EX_TOOLWINDOW: hide from taskbar and Alt+Tab.
        var exStyle = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)(exStyle | WS_EX_TOOLWINDOW));

        // Windows 11 rounded corners.
        var preference = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));

        // Size the window.
        appWindow.Resize(new global::Windows.Graphics.SizeInt32(width, height));

        // Position at the bottom-right of the work area, flush against the taskbar.
        var area = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var workArea = area.WorkArea;
        appWindow.Move(new global::Windows.Graphics.PointInt32(
            workArea.X + workArea.Width - width - TrayMargin,
            workArea.Y + workArea.Height - height));

        return hwnd;
    }
}
