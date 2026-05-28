// Placeholder for the platform-neutral core. Ported from Sources/WhatCableCore in PR 2.
namespace WhatCable.Core;

/// <summary>
/// Marker type that lets the assembly load while project skeletons are being filled in.
/// Replaced by the ported models (CableSnapshot, PortSummary, USBPDVDO, etc.) in PR 2.
/// </summary>
internal static class CoreAssemblyMarker
{
    public const string Version = "0.0.0-scaffold";
}
