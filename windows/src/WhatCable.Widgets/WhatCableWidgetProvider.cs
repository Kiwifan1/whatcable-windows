using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Providers;
using WhatCable.Widgets.Core;
using HostWidgetSize = Microsoft.Windows.Widgets.WidgetSize;
using CardWidgetSize = WhatCable.Widgets.Core.WidgetSize;

namespace WhatCable.Widgets;

/// <summary>
/// The WhatCable Windows 11 widget provider. Implements <see cref="IWidgetProvider"/> so the Widgets
/// Board (and the 23H2+ lock screen) can pin the cable-status widget, and listens on the snapshot
/// named pipe so the WinUI app can push live USB-C state. Adaptive Cards are built by the testable
/// <see cref="AdaptiveCardBuilder"/> in <c>WhatCable.Widgets.Core</c>.
/// </summary>
[ComVisible(true)]
[ComDefaultInterface(typeof(IWidgetProvider))]
[Guid(WidgetCatalog.ProviderClassId)]
public sealed class WhatCableWidgetProvider : IWidgetProvider
{
    // Pinned widgets the host is tracking: widget id -> current size.
    private static readonly ConcurrentDictionary<string, CardWidgetSize> ActiveWidgets = new();

    // The most recent payload pushed by the app; rendered for new/resized widgets and on update.
    private static volatile WidgetPayload _latestPayload = new();

    private static readonly WidgetSnapshotPipeServer PipeServer = StartPipeServer();

    private static WidgetSnapshotPipeServer StartPipeServer()
    {
        var server = new WidgetSnapshotPipeServer();
        server.PayloadReceived += (_, payload) =>
        {
            _latestPayload = payload;
            UpdateAllWidgets();
        };
        server.Start();
        return server;
    }

    public WhatCableWidgetProvider() => RecoverPinnedWidgets();

    private static bool _recovered;

    /// <summary>Re-discovers widgets pinned in a previous session so updates resume after a restart.</summary>
    private static void RecoverPinnedWidgets()
    {
        if (_recovered)
        {
            return;
        }

        try
        {
            var manager = WidgetManager.GetDefault();
            foreach (var info in manager.GetWidgetInfos())
            {
                var context = info.WidgetContext;
                if (context.DefinitionId == WidgetCatalog.CableStatusDefinitionId)
                {
                    ActiveWidgets[context.Id] = MapSize(context.Size);
                }
                else
                {
                    // A widget we no longer provide: let the host clean it up.
                    manager.DeleteWidget(context.Id);
                }
            }
        }
        catch
        {
            // Recovery is best-effort; a missing manager simply means no widgets are pinned yet.
        }
        finally
        {
            _recovered = true;
        }
    }

    public void CreateWidget(WidgetContext widgetContext)
    {
        ActiveWidgets[widgetContext.Id] = MapSize(widgetContext.Size);
        UpdateWidget(widgetContext.Id);
    }

    public void DeleteWidget(string widgetId, string customState) => ActiveWidgets.TryRemove(widgetId, out _);

    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        // The status widget is read-only; re-render so a tap refreshes the displayed snapshot.
        UpdateWidget(actionInvokedArgs.WidgetContext.Id);
    }

    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        var context = contextChangedArgs.WidgetContext;
        ActiveWidgets[context.Id] = MapSize(context.Size);
        UpdateWidget(context.Id);
    }

    public void Activate(WidgetContext widgetContext)
    {
        ActiveWidgets[widgetContext.Id] = MapSize(widgetContext.Size);
        UpdateWidget(widgetContext.Id);
    }

    public void Deactivate(string widgetId)
    {
        // Host has stopped listening; keep the registration but pause pushing updates.
    }

    private static void UpdateAllWidgets()
    {
        foreach (var widgetId in ActiveWidgets.Keys)
        {
            UpdateWidget(widgetId);
        }
    }

    private static void UpdateWidget(string widgetId)
    {
        if (!ActiveWidgets.TryGetValue(widgetId, out var size))
        {
            return;
        }

        try
        {
            var options = new WidgetUpdateRequestOptions(widgetId)
            {
                Template = AdaptiveCardBuilder.Build(_latestPayload, size),
                Data = "{}",
            };
            WidgetManager.GetDefault().UpdateWidget(options);
        }
        catch
        {
            // The host may have removed the widget between calls; ignore transient update failures.
        }
    }

    private static CardWidgetSize MapSize(HostWidgetSize hostSize) => hostSize switch
    {
        HostWidgetSize.Small => CardWidgetSize.Small,
        HostWidgetSize.Large => CardWidgetSize.Large,
        _ => CardWidgetSize.Medium,
    };
}
