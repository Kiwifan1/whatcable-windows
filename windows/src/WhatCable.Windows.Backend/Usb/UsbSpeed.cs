namespace WhatCable.Windows.Backend.Usb;

/// <summary>
/// Negotiated USB link speed for a connected device, derived from
/// <c>USB_NODE_CONNECTION_INFORMATION_EX</c> / <c>USB_NODE_CONNECTION_INFORMATION_EX_V2</c>.
/// </summary>
public enum UsbSpeed
{
    Unknown = 0,
    Low,
    Full,
    High,
    SuperSpeed,
    SuperSpeedPlus,
    SuperSpeedPlus20,
}

/// <summary>Human-readable labels for <see cref="UsbSpeed"/>, matching the macOS JSON wording.</summary>
public static class UsbSpeedLabels
{
    public static string Describe(UsbSpeed speed) => speed switch
    {
        UsbSpeed.Low => "USB 1.0 (1.5 Mbps)",
        UsbSpeed.Full => "USB 1.1 (12 Mbps)",
        UsbSpeed.High => "USB 2.0 (480 Mbps)",
        UsbSpeed.SuperSpeed => "USB 3.2 Gen 1 (5 Gbps)",
        UsbSpeed.SuperSpeedPlus => "USB 3.2 Gen 2 (10 Gbps)",
        UsbSpeed.SuperSpeedPlus20 => "USB 3.2 Gen 2x2 (20 Gbps)",
        _ => "Unknown",
    };
}
