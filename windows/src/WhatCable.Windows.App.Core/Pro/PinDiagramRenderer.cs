using System.Globalization;
using System.Text;
using WhatCable.Core;

namespace WhatCable.Windows.App.Core.Pro;

/// <summary>Cable class inferred from the active alternate modes and the cable e-marker speed.</summary>
public enum CableClass
{
    /// <summary>USB 2.0 only (no SuperSpeed pairs routed).</summary>
    Usb2Only,

    /// <summary>USB 3.x SuperSpeed data, no display.</summary>
    Usb3,

    /// <summary>DisplayPort Alt Mode active.</summary>
    DisplayPort,

    /// <summary>Thunderbolt / USB4 active.</summary>
    Thunderbolt,
}

/// <summary>
/// The result of rendering a pin diagram for a port: the pin map, an inline SVG, and graceful
/// availability metadata so the UI can degrade when the hardware can't supply the data.
/// </summary>
public sealed record PinDiagram
{
    /// <summary>True when a diagram could be derived from UCSI data.</summary>
    public bool Available { get; init; }

    /// <summary>Machine-readable reason the diagram is unavailable (e.g. <c>ucsi_not_supported</c>).</summary>
    public string? UnavailableReason { get; init; }

    /// <summary>Inferred cable class driving the SVG template, when available.</summary>
    public CableClass? CableClass { get; init; }

    /// <summary>The 24-pin map, when available.</summary>
    public UsbCPinMap? PinMap { get; init; }

    /// <summary>Self-contained inline SVG markup, when available.</summary>
    public string? Svg { get; init; }

    /// <summary>Short human-readable summary of the routed protocols (e.g. "USB 3 + DP (2 lanes)").</summary>
    public string? SignalSummary { get; init; }

    public static PinDiagram Unavailable(string reason) => new() { Available = false, UnavailableReason = reason };
}

/// <summary>
/// Renders USB-C pin diagrams (Pro) from UCSI <c>GET_ALTERNATE_MODES</c> + cable VDO data. Each
/// cable class gets its own SVG template (accent colour + caption); the pin grid itself is shared.
/// </summary>
public static class PinDiagramRenderer
{
    /// <summary>Emitted when the host PC has no UCSI data to derive pin routing from.</summary>
    public const string ReasonNotUcsi = "ucsi_not_supported";

    /// <summary>Emitted when a UCSI port has nothing attached, so no routing is active.</summary>
    public const string ReasonNotConnected = "no_partner_connected";

    /// <summary>Renders the diagram for a UCSI port view, or an unavailable result.</summary>
    public static PinDiagram Render(UcsiPortView? view)
    {
        if (view is null)
        {
            return PinDiagram.Unavailable(ReasonNotUcsi);
        }

        if (!view.Connected)
        {
            return PinDiagram.Unavailable(ReasonNotConnected);
        }

        var cableClass = ClassifyCable(view);
        var (dpLanes, usb3) = SignalLayout(view, cableClass);
        var pinMap = UsbCPinMap.Build(dpLanes, usb3);
        var summary = DescribeSignals(dpLanes, usb3);

        return new PinDiagram
        {
            Available = true,
            CableClass = cableClass,
            PinMap = pinMap,
            SignalSummary = summary,
            Svg = BuildSvg(pinMap, cableClass, summary),
        };
    }

    internal static CableClass ClassifyCable(UcsiPortView view)
    {
        if (view.ThunderboltActive)
        {
            return CableClass.Thunderbolt;
        }

        if (view.DisplayPortActive)
        {
            return CableClass.DisplayPort;
        }

        var usb3Capable = view.Usb3Capable || view.CableSpeed is >= CableSpeed.Usb32Gen1;
        return usb3Capable ? CableClass.Usb3 : CableClass.Usb2Only;
    }

    private static (int DpLanes, bool Usb3) SignalLayout(UcsiPortView view, CableClass cableClass)
    {
        var usb3 = view.Usb3Capable || view.CableSpeed is >= CableSpeed.Usb32Gen1;
        return cableClass switch
        {
            // Thunderbolt/USB4 uses all four SuperSpeed pairs for the link; show them as USB 3 SS lanes.
            CableClass.Thunderbolt => (0, true),

            // DisplayPort Alt Mode: 2 DP lanes + USB 3 when the cable still carries SuperSpeed
            // (pin assignment D), otherwise 4 DP lanes (pin assignment C/E).
            CableClass.DisplayPort => usb3 ? (2, true) : (4, false),

            CableClass.Usb3 => (0, true),
            _ => (0, false),
        };
    }

    private static string DescribeSignals(int dpLanes, bool usb3)
    {
        var hasDp = dpLanes > 0;
        return (usb3, hasDp) switch
        {
            (true, true) => $"USB 3 + DP ({dpLanes} lane{(dpLanes == 1 ? "" : "s")})",
            (true, false) => "USB 3",
            (false, true) => $"DP ({dpLanes} lane{(dpLanes == 1 ? "" : "s")})",
            _ => "USB 2.0 only",
        };
    }

    private static string Accent(CableClass cableClass) => cableClass switch
    {
        CableClass.Thunderbolt => "#7B61FF",
        CableClass.DisplayPort => "#0A84FF",
        CableClass.Usb3 => "#30D158",
        _ => "#8E8E93",
    };

    private static string Caption(CableClass cableClass) => cableClass switch
    {
        CableClass.Thunderbolt => "Thunderbolt / USB4",
        CableClass.DisplayPort => "DisplayPort Alt Mode",
        CableClass.Usb3 => "USB 3 SuperSpeed",
        _ => "USB 2.0",
    };

    private static string Fill(PinSignal signal, string accent) => signal switch
    {
        PinSignal.Inactive => "#2C2C2E",
        PinSignal.Ground => "#48484A",
        PinSignal.Vbus => "#FF453A",
        PinSignal.Cc => "#FFD60A",
        PinSignal.Usb2 => "#64D2FF",
        PinSignal.Usb3 or PinSignal.DpLane or PinSignal.DpAux => accent,
        _ => "#2C2C2E",
    };

    /// <summary>Builds a self-contained SVG (no external refs) for safe embedding in any host.</summary>
    internal static string BuildSvg(UsbCPinMap map, CableClass cableClass, string summary)
    {
        const int pinW = 26;
        const int pinH = 26;
        const int gap = 4;
        const int marginX = 12;
        const int marginTop = 36;
        const int rowGap = 10;

        var accent = Accent(cableClass);
        var width = (marginX * 2) + (12 * pinW) + (11 * gap);
        var height = marginTop + (2 * pinH) + rowGap + 28;

        var sb = new StringBuilder();
        sb.Append(string.Create(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\">"));
        sb.Append(string.Create(CultureInfo.InvariantCulture,
            $"<text x=\"{marginX}\" y=\"20\" font-family=\"Segoe UI, sans-serif\" font-size=\"14\" font-weight=\"600\" fill=\"{accent}\">{Escape(Caption(cableClass))}</text>"));

        AppendRow(sb, map.TopRow, marginX, marginTop, pinW, pinH, gap, accent);
        AppendRow(sb, map.BottomRow, marginX, marginTop + pinH + rowGap, pinW, pinH, gap, accent);

        var captionY = marginTop + (2 * pinH) + rowGap + 20;
        sb.Append(string.Create(CultureInfo.InvariantCulture,
            $"<text x=\"{marginX}\" y=\"{captionY}\" font-family=\"Segoe UI, sans-serif\" font-size=\"12\" fill=\"#AEAEB2\">{Escape(summary)}</text>"));
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static void AppendRow(
        StringBuilder sb, IReadOnlyList<UsbCPin> row, int x0, int y, int pinW, int pinH, int gap, string accent)
    {
        for (var i = 0; i < row.Count; i++)
        {
            var pin = row[i];
            var x = x0 + (i * (pinW + gap));
            var fill = Fill(pin.Signal, accent);
            sb.Append(string.Create(CultureInfo.InvariantCulture,
                $"<rect x=\"{x}\" y=\"{y}\" width=\"{pinW}\" height=\"{pinH}\" rx=\"3\" fill=\"{fill}\"><title>{Escape(pin.Id)}: {Escape(pin.Label)}</title></rect>"));
        }
    }

    private static string Escape(string value)
        => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
