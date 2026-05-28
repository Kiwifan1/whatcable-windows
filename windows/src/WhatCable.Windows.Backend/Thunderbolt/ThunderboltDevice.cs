using System.Text.Json.Serialization;

namespace WhatCable.Windows.Backend.Thunderbolt;

/// <summary>
/// A node in the Thunderbolt/USB4 device chain, as emitted in the CLI <c>thunderbolt</c>
/// section.
/// </summary>
/// <remarks>
/// Per-lane link speeds are intentionally absent: Windows exposes no public API to read
/// the negotiated per-lane Thunderbolt/USB4 lane speeds (this requires vendor firmware
/// interfaces), so <see cref="PerLaneSpeedsAvailable"/> is always <c>false</c>. The
/// <see cref="UnavailableReason"/> documents this Windows API limitation.
/// </remarks>
public sealed record ThunderboltDevice
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("instanceId")] public string InstanceId { get; init; } = string.Empty;
    [JsonPropertyName("enumerationIndex")] public int EnumerationIndex { get; init; }
    [JsonPropertyName("perLaneSpeedsAvailable")] public bool PerLaneSpeedsAvailable { get; init; }
    [JsonPropertyName("unavailable_reason")] public string? UnavailableReason { get; init; }
}
