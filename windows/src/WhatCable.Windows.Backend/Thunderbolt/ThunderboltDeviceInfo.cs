namespace WhatCable.Windows.Backend.Thunderbolt;

/// <summary>
/// A Thunderbolt/USB4 controller or device discovered via SetupAPI, captured as a
/// platform-neutral record so the chain mapping can be unit-tested against recorded
/// device lists.
/// </summary>
public sealed record ThunderboltDeviceInfo
{
    /// <summary>Friendly device name, if it could be read.</summary>
    public string? Name { get; init; }

    /// <summary>Device instance id (stable identifier from SetupAPI).</summary>
    public string InstanceId { get; init; } = string.Empty;

    /// <summary>Device-interface path of the controller/device.</summary>
    public string InterfacePath { get; init; } = string.Empty;
}
