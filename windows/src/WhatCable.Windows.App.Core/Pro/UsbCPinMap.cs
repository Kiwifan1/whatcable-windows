namespace WhatCable.Windows.App.Core.Pro;

/// <summary>
/// What signal is routed through a USB-C pin. Mirrors the macOS <c>USBCPinMap.Signal</c> so the
/// Windows pin diagram speaks the same vocabulary, but it is derived from UCSI
/// <c>GET_ALTERNATE_MODES</c> + cable VDO data rather than IOKit's pin-configuration dictionary
/// (which has no Windows equivalent).
/// </summary>
public enum PinSignal
{
    Inactive,
    Ground,
    Vbus,
    Cc,
    Usb2,
    Usb3,
    DpLane,
    DpAux,
}

/// <summary>A single physical pin on the connector.</summary>
public sealed record UsbCPin(string Id, PinSignal Signal)
{
    /// <summary>Short label for the routed signal.</summary>
    public string Label => Signal switch
    {
        PinSignal.Inactive => "—",
        PinSignal.Ground => "GND",
        PinSignal.Vbus => "VBUS",
        PinSignal.Cc => "CC",
        PinSignal.Usb2 => "USB 2.0",
        PinSignal.Usb3 => "USB 3",
        PinSignal.DpLane => "DP",
        PinSignal.DpAux => "AUX",
        _ => "?",
    };

    /// <summary>True when this pin carries a dynamic high-speed protocol (USB 3, DP, or sideband).</summary>
    public bool IsDynamic => Signal is PinSignal.Usb3 or PinSignal.DpLane or PinSignal.DpAux;
}

/// <summary>
/// The 24 pins of a USB-C connector, laid out as two rows so the visual diagram lines up. Top row
/// is A1..A12; bottom row is B12..B1 (visual left-to-right), matching the macOS layout.
/// </summary>
public sealed record UsbCPinMap
{
    public IReadOnlyList<UsbCPin> TopRow { get; init; } = Array.Empty<UsbCPin>();
    public IReadOnlyList<UsbCPin> BottomRow { get; init; } = Array.Empty<UsbCPin>();

    public IEnumerable<UsbCPin> AllPins => TopRow.Concat(BottomRow);

    /// <summary>True when at least one pin carries a dynamic signal.</summary>
    public bool HasActivity => AllPins.Any(p => p.IsDynamic);

    /// <summary>
    /// Builds a pin map for the given high-speed signal layout. <paramref name="dpLanes"/> is the
    /// number of DisplayPort lanes routed (0, 2 or 4); <paramref name="usb3"/> indicates a USB 3
    /// SuperSpeed pair is present.
    /// </summary>
    public static UsbCPinMap Build(int dpLanes, bool usb3)
    {
        // The four SuperSpeed pin pairs (tx1/rx1/tx2/rx2). With 4 DP lanes all four carry DP; with
        // 2 DP lanes the second pair carries DP and the first carries USB 3 (pin assignment D).
        var tx1 = dpLanes >= 4 ? PinSignal.DpLane : (usb3 ? PinSignal.Usb3 : PinSignal.Inactive);
        var rx1 = tx1;
        var tx2 = dpLanes >= 2 ? PinSignal.DpLane : (usb3 ? PinSignal.Usb3 : PinSignal.Inactive);
        var rx2 = tx2;
        var sbu = dpLanes > 0 ? PinSignal.DpAux : PinSignal.Inactive;

        var topRow = new[]
        {
            new UsbCPin("A1", PinSignal.Ground),
            new UsbCPin("A2", tx1),
            new UsbCPin("A3", tx1),
            new UsbCPin("A4", PinSignal.Vbus),
            new UsbCPin("A5", PinSignal.Cc),
            new UsbCPin("A6", PinSignal.Usb2),
            new UsbCPin("A7", PinSignal.Usb2),
            new UsbCPin("A8", sbu),
            new UsbCPin("A9", PinSignal.Vbus),
            new UsbCPin("A10", rx2),
            new UsbCPin("A11", rx2),
            new UsbCPin("A12", PinSignal.Ground),
        };

        var bottomRow = new[]
        {
            new UsbCPin("B12", PinSignal.Ground),
            new UsbCPin("B11", rx1),
            new UsbCPin("B10", rx1),
            new UsbCPin("B9", PinSignal.Vbus),
            new UsbCPin("B8", sbu),
            new UsbCPin("B7", PinSignal.Usb2),
            new UsbCPin("B6", PinSignal.Usb2),
            new UsbCPin("B5", PinSignal.Cc),
            new UsbCPin("B4", PinSignal.Vbus),
            new UsbCPin("B3", tx2),
            new UsbCPin("B2", tx2),
            new UsbCPin("B1", PinSignal.Ground),
        };

        return new UsbCPinMap { TopRow = topRow, BottomRow = bottomRow };
    }
}
