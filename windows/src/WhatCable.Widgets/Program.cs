// Widget Provider COM server entry point.
//
// The Windows 11 Widgets Board host launches this executable with
// "-RegisterProcessAsComServer" when it needs the WhatCable widget provider. We register the
// IWidgetProvider COM class object, then block until the host releases the last pinned widget.
using WinRT;

namespace WhatCable.Widgets;

internal static class Program
{
    [MTAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] != "-RegisterProcessAsComServer")
        {
            // Not launched to service the widget host; nothing to do.
            return 0;
        }

        ComWrappersSupport.InitializeComWrappers();

        using var registration = WidgetProviderRegistration<WhatCableWidgetProvider>.Register();

        // Wait until the host has disposed of the last widget provider before exiting.
        using var disposedEvent = registration.GetDisposedEvent();
        disposedEvent.WaitOne();

        return 0;
    }
}
