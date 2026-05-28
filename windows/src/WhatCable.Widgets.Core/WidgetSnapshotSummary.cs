using WhatCable.Core;

namespace WhatCable.Widgets.Core;

/// <summary>One row in the per-port widget summary table.</summary>
public sealed record PortSummaryRow(string Name, bool Active, string Status, string Detail);

/// <summary>
/// Projects a <see cref="Snapshot"/> into the small set of strings the Adaptive Card templates
/// bind to: the charging headline, the active port's link speed, and a per-port row list. Pure and
/// UI-free so it can be unit-tested without the widget host.
/// </summary>
public static class WidgetSnapshotSummary
{
    /// <summary>Total number of ports in the snapshot.</summary>
    public static int PortCount(Snapshot snapshot) => snapshot.Ports.Count;

    /// <summary>Number of ports with an active connection.</summary>
    public static int ActivePortCount(Snapshot snapshot) => snapshot.Ports.Count(p => p.ConnectionActive);

    /// <summary>
    /// A one-line charging headline derived from the adapter section: the negotiated wattage when
    /// charging, otherwise a "not charging" / "on battery" message.
    /// </summary>
    public static string ChargingStatus(Snapshot snapshot)
    {
        var watts = snapshot.Adapter?.Watts;
        if (watts is > 0)
        {
            var source = snapshot.Adapter?.Description ?? snapshot.Adapter?.Source;
            return string.IsNullOrWhiteSpace(source)
                ? $"Charging · {watts} W"
                : $"Charging · {watts} W · {source}";
        }

        return "Not charging";
    }

    /// <summary>
    /// The most descriptive link speed available for the first active port: the cable e-marker
    /// speed if known, else the connected device's negotiated speed, else its headline. Returns an
    /// empty string when nothing is connected.
    /// </summary>
    public static string ActivePortSpeed(Snapshot snapshot)
    {
        var port = snapshot.Ports.FirstOrDefault(p => p.ConnectionActive);
        if (port is null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(port.Cable?.Speed))
        {
            return port.Cable!.Speed!;
        }

        if (!string.IsNullOrWhiteSpace(port.Device?.Speed))
        {
            return port.Device!.Speed;
        }

        return port.Headline;
    }

    /// <summary>The per-port rows for the medium widget's summary table.</summary>
    public static IReadOnlyList<PortSummaryRow> PortRows(Snapshot snapshot)
        => snapshot.Ports
            .Select(p => new PortSummaryRow(
                Name: p.Name,
                Active: p.ConnectionActive,
                Status: p.ConnectionActive
                    ? (string.IsNullOrWhiteSpace(p.Headline) ? "Connected" : p.Headline)
                    : "Nothing connected",
                Detail: PortDetail(p)))
            .ToList();

    private static string PortDetail(Port port)
    {
        if (!port.ConnectionActive)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(port.Cable?.Speed))
        {
            return port.Cable!.Speed!;
        }

        return string.IsNullOrWhiteSpace(port.Subtitle) ? port.Device?.Speed ?? string.Empty : port.Subtitle;
    }
}
