using WhatCable.Core;
using WhatCable.Windows.App.Core.Services;

namespace WhatCable.Windows.App.Core.ViewModels;

/// <summary>
/// Pure (UI-thread-free) diff of two consecutive <see cref="Snapshot"/>s into the connect/disconnect
/// events the app turns into toast notifications. Kept separate from <see cref="TrayViewModel"/> so
/// the detection rules can be unit-tested directly.
/// </summary>
public static class SnapshotChangeDetector
{
    public static IReadOnlyList<PortChange> Detect(Snapshot? previous, Snapshot current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (previous is null)
        {
            return Array.Empty<PortChange>();
        }

        var changes = new List<PortChange>();

        // Charger (system adapter) transitions.
        var hadAdapter = HasAdapter(previous.Adapter);
        var hasAdapter = HasAdapter(current.Adapter);
        if (!hadAdapter && hasAdapter)
        {
            changes.Add(new PortChange(PortChangeKind.ChargerConnected, AdapterName(current.Adapter), AdapterDetail(current.Adapter)));
        }
        else if (hadAdapter && !hasAdapter)
        {
            changes.Add(new PortChange(PortChangeKind.ChargerDisconnected, AdapterName(previous.Adapter), AdapterDetail(previous.Adapter)));
        }

        // Per-port device count transitions, matched by port name.
        var before = previous.Ports.ToDictionary(p => p.Name, CountDevices);
        foreach (var port in current.Ports)
        {
            var now = CountDevices(port);
            before.TryGetValue(port.Name, out var then);
            if (now > then)
            {
                changes.Add(new PortChange(PortChangeKind.DeviceConnected, port.Name, port.Headline));
            }
            else if (now < then)
            {
                changes.Add(new PortChange(PortChangeKind.DeviceDisconnected, port.Name, port.Headline));
            }
        }

        return changes;
    }

    private static bool HasAdapter(AdapterSnapshot? adapter)
        => adapter is not null && (adapter.Watts is > 0 || !string.IsNullOrEmpty(adapter.Source));

    /// <summary>A non-empty label for the charger, falling back to a generic name when the
    /// adapter reports no source so notifications never show blank text.</summary>
    private static string AdapterName(AdapterSnapshot? adapter)
        => !string.IsNullOrEmpty(adapter?.Source) ? adapter!.Source! : "Power adapter";

    /// <summary>Optional watts detail so charger notifications carry meaningful context.</summary>
    private static string? AdapterDetail(AdapterSnapshot? adapter)
        => adapter?.Watts is > 0 ? $"{adapter.Watts} W" : null;

    private static int CountDevices(Port port)
        => port.Device is { } root ? CountNodes(root) : 0;

    private static int CountNodes(DeviceNode node)
    {
        var count = 1;
        if (node.Children is { } children)
        {
            foreach (var child in children)
            {
                count += CountNodes(child);
            }
        }

        return count;
    }
}
