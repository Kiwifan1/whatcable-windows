using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace WhatCable.Windows.App.Helpers;

/// <summary>
/// Styles a WinUI 3 <see cref="Window"/> as a tray popup: no taskbar entry, no title bar,
/// positioned above the system tray, and auto-dismissed on focus loss.
/// </summary>
internal static class PopupWindowHelper
{
    private const int GWL_EXSTYLE = -20;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const int TrayMargin = 12;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    private const uint ABM_GETTASKBARPOS = 0x00000005;

    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public int lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    /// <summary>
    /// Applies tray-popup styling to <paramref name="window"/>:
    /// <list type="bullet">
    ///   <item>Removes the title bar</item>
    ///   <item>Hides from the taskbar and Alt+Tab via <c>WS_EX_TOOLWINDOW</c></item>
    ///   <item>Sizes the window to <paramref name="width"/> x <paramref name="height"/> (logical pixels, DPI-scaled automatically)</item>
    ///   <item>Positions the window directly above the system tray area</item>
    ///   <item>Wires the <c>Activated</c> event to close on focus loss</item>
    /// </list>
    /// </summary>
    public static void ApplyTrayPopupStyle(Window window, int width, int height)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // 1. Remove title bar, disable resize/minimize/maximize.
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(true, false);
        }

        // 2. WS_EX_TOOLWINDOW: hide from taskbar and Alt+Tab.
        var exStyle = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)(exStyle | WS_EX_TOOLWINDOW));

        // 3. Scale logical pixels to device pixels for the target monitor's DPI.
        var dpi = GetDpiForWindow(hwnd);
        var scale = dpi > 0 ? dpi / 96.0 : 1.0;
        var scaledWidth = (int)(width * scale);
        var scaledHeight = (int)(height * scale);
        var scaledMargin = (int)(TrayMargin * scale);

        appWindow.Resize(new global::Windows.Graphics.SizeInt32(scaledWidth, scaledHeight));

        // 4. Position above the tray.
        PositionAboveTray(appWindow, windowId, scaledWidth, scaledHeight, scaledMargin);

        // 5. Auto-dismiss on focus loss.
        window.Activated += (_, args) =>
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                window.Close();
            }
        };
    }

    private static void PositionAboveTray(
        AppWindow appWindow, WindowId windowId, int width, int height, int margin)
    {
        if (TryGetTaskbarPosition(out var abd))
        {
            int x, y;
            switch (abd.uEdge)
            {
                case 3: // ABE_BOTTOM
                    x = abd.rc.Right - width - margin;
                    y = abd.rc.Top - height - margin;
                    break;
                case 1: // ABE_TOP
                    x = abd.rc.Right - width - margin;
                    y = abd.rc.Bottom + margin;
                    break;
                case 0: // ABE_LEFT
                    x = abd.rc.Right + margin;
                    y = abd.rc.Bottom - height - margin;
                    break;
                case 2: // ABE_RIGHT
                    x = abd.rc.Left - width - margin;
                    y = abd.rc.Bottom - height - margin;
                    break;
                default:
                    PositionAtWorkAreaCorner(appWindow, windowId, width, height, margin);
                    return;
            }

            appWindow.Move(new global::Windows.Graphics.PointInt32(x, y));
            return;
        }

        PositionAtWorkAreaCorner(appWindow, windowId, width, height, margin);
    }

    private static bool TryGetTaskbarPosition(out APPBARDATA abd)
    {
        abd = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>() };
        var result = SHAppBarMessage(ABM_GETTASKBARPOS, ref abd);
        return result != 0
            && (abd.rc.Right - abd.rc.Left > 0 || abd.rc.Bottom - abd.rc.Top > 0);
    }

    private static void PositionAtWorkAreaCorner(
        AppWindow appWindow, WindowId windowId, int width, int height, int margin)
    {
        var area = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var workArea = area.WorkArea;
        appWindow.Move(new global::Windows.Graphics.PointInt32(
            workArea.X + workArea.Width - width - margin,
            workArea.Y + workArea.Height - height - margin));
    }
}
