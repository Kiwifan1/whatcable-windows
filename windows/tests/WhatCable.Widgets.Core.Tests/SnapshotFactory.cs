using WhatCable.Core;

namespace WhatCable.Widgets.Core.Tests;

/// <summary>Builders mirroring the App.Core test fixtures, for terse snapshot construction.</summary>
internal static class SnapshotFactory
{
    public static DeviceNode Device(string name, string speed = "SuperSpeed") => new()
    {
        Name = name,
        Speed = speed,
    };

    public static Cable Cable(string speed) => new() { Speed = speed };

    public static Port Port(
        string name,
        bool active,
        string headline = "",
        string subtitle = "",
        Cable? cable = null,
        DeviceNode? device = null) => new()
    {
        Name = name,
        ClassName = "USB4PlatformPolicyClient",
        ConnectionActive = active,
        Status = active ? "Active" : "Nothing connected",
        Headline = headline,
        Subtitle = subtitle,
        Cable = cable,
        Device = device,
    };

    public static Snapshot Snapshot(AdapterSnapshot? adapter, params Port[] ports) => new()
    {
        Version = "0.26.0",
        Adapter = adapter,
        Ports = ports,
    };
}
