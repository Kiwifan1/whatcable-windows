namespace WhatCable.Windows.Backend.Usb;

/// <summary>
/// A device discovered on a USB hub port, captured as a platform-neutral record so the
/// topology mapping can be unit-tested against recorded device trees.
/// </summary>
public sealed record UsbDeviceInfo
{
    /// <summary>Product name (USB string descriptor), if it could be read.</summary>
    public string? Name { get; init; }

    public int VendorId { get; init; }

    public int ProductId { get; init; }

    /// <summary>Negotiated link speed for this connection.</summary>
    public UsbSpeed Speed { get; init; }

    /// <summary>Hub-relative location, e.g. the 1-based port index on the parent hub.</summary>
    public int PortNumber { get; init; }

    /// <summary>True when this node is itself a hub with downstream devices.</summary>
    public bool IsHub { get; init; }

    /// <summary>Devices attached below this node (populated when <see cref="IsHub"/> is true).</summary>
    public IReadOnlyList<UsbDeviceInfo> Children { get; init; } = Array.Empty<UsbDeviceInfo>();
}

/// <summary>
/// A USB host controller and the device tree hanging off its root hub.
/// </summary>
public sealed record UsbHostController
{
    public string Name { get; init; } = string.Empty;

    /// <summary>Device-interface path of the controller (used as a stable identifier).</summary>
    public string InterfacePath { get; init; } = string.Empty;

    /// <summary>Top-level devices connected to the controller's root hub.</summary>
    public IReadOnlyList<UsbDeviceInfo> Devices { get; init; } = Array.Empty<UsbDeviceInfo>();
}
