using WhatCable.Core;

namespace WhatCable.Windows.App.Core.Pro;

/// <summary>
/// System-wide liquid-detection state aggregated across all UCSI connectors (Pro, UCSI 2.0 only).
/// </summary>
public sealed record LiquidDetectionState
{
    /// <summary>Emitted when no connector implements the UCSI 2.0 liquid-detection notification.</summary>
    public const string ReasonUnsupported = "ucsi20_liquid_detection_unavailable";

    /// <summary>True when at least one connector exposes the liquid-detection field.</summary>
    public bool Supported { get; init; }

    /// <summary>True when liquid is currently detected on any connector.</summary>
    public bool Detected { get; init; }

    /// <summary>1-based indices of the connectors currently reporting liquid, in port order.</summary>
    public IReadOnlyList<int> AffectedPorts { get; init; } = Array.Empty<int>();

    /// <summary>Machine-readable reason when the feature is unavailable on this hardware.</summary>
    public string? UnavailableReason => Supported ? null : ReasonUnsupported;

    /// <summary>Aggregates liquid-detection state from the ports of a snapshot.</summary>
    public static LiquidDetectionState FromSnapshot(Snapshot? snapshot)
    {
        if (snapshot is null)
        {
            return new LiquidDetectionState();
        }

        var supported = false;
        var affected = new List<int>();
        var index = 0;
        foreach (var port in snapshot.Ports)
        {
            index++;
            var view = UcsiPortView.FromPort(port);
            if (view is null || !view.LiquidDetectionSupported)
            {
                continue;
            }

            supported = true;
            if (view.LiquidDetected == true)
            {
                affected.Add(index);
            }
        }

        return new LiquidDetectionState
        {
            Supported = supported,
            Detected = affected.Count > 0,
            AffectedPorts = affected,
        };
    }
}
