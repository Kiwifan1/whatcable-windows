using System.Text.Json.Serialization;

namespace WhatCable.Core;

public sealed record Port
{
    [JsonPropertyName("name"), JsonPropertyOrder(0)] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("type"), JsonPropertyOrder(1)] public string? Type { get; init; }
    [JsonPropertyName("className"), JsonPropertyOrder(2)] public string ClassName { get; init; } = string.Empty;
    [JsonPropertyName("connectionActive"), JsonPropertyOrder(3)] public bool ConnectionActive { get; init; }
    [JsonPropertyName("pdCapable"), JsonPropertyOrder(4)] public bool PdCapable { get; init; }
    [JsonPropertyName("status"), JsonPropertyOrder(5)] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("headline"), JsonPropertyOrder(6)] public string Headline { get; init; } = string.Empty;
    [JsonPropertyName("subtitle"), JsonPropertyOrder(7)] public string Subtitle { get; init; } = string.Empty;
    [JsonPropertyName("bullets"), JsonPropertyOrder(8)] public IReadOnlyList<string> Bullets { get; init; } = Array.Empty<string>();
    [JsonPropertyName("powerSources"), JsonPropertyOrder(9)] public IReadOnlyList<ChargerProfile> PowerSources { get; init; } = Array.Empty<ChargerProfile>();
    [JsonPropertyName("cable"), JsonPropertyOrder(10)] public Cable? Cable { get; init; }
    [JsonPropertyName("device"), JsonPropertyOrder(11)] public DeviceNode? Device { get; init; }
}

public sealed record Cable
{
    [JsonPropertyName("endpoint"), JsonPropertyOrder(0)] public string Endpoint { get; init; } = string.Empty;
    [JsonPropertyName("vendorID"), JsonPropertyOrder(1)] public int VendorId { get; init; }
    [JsonPropertyName("vendorName"), JsonPropertyOrder(2)] public string? VendorName { get; init; }
    [JsonPropertyName("curatedBrand"), JsonPropertyOrder(3)] public string? CuratedBrand { get; init; }
    [JsonPropertyName("speed"), JsonPropertyOrder(4)] public string? Speed { get; init; }
    [JsonPropertyName("currentRating"), JsonPropertyOrder(5)] public string? CurrentRating { get; init; }
    [JsonPropertyName("maxVolts"), JsonPropertyOrder(6)] public int? MaxVolts { get; init; }
    [JsonPropertyName("maxWatts"), JsonPropertyOrder(7)] public int? MaxWatts { get; init; }
    [JsonPropertyName("type"), JsonPropertyOrder(8)] public string? Type { get; init; }
    [JsonPropertyName("trustFlags"), JsonPropertyOrder(9)] public IReadOnlyList<TrustFlag>? TrustFlags { get; init; }
}

public sealed record TrustFlag
{
    [JsonPropertyName("code")] public string Code { get; init; } = string.Empty;
    [JsonPropertyName("title")] public string Title { get; init; } = string.Empty;
    [JsonPropertyName("detail")] public string Detail { get; init; } = string.Empty;

    public TrustFlag() { }
    public TrustFlag(string code, string title, string detail) => (Code, Title, Detail) = (code, title, detail);
}

public sealed record DeviceNode
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("vendorID")] public int VendorId { get; init; }
    [JsonPropertyName("productID")] public int ProductId { get; init; }
    [JsonPropertyName("vendorName")] public string? VendorName { get; init; }
    [JsonPropertyName("speed")] public string Speed { get; init; } = string.Empty;
    [JsonPropertyName("locationID")] public string LocationId { get; init; } = string.Empty;
    [JsonPropertyName("children")] public IReadOnlyList<DeviceNode>? Children { get; init; }
}

public sealed record ChargerProfile
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("maxPowerW")] public int MaxPowerW { get; init; }
    [JsonPropertyName("options")] public IReadOnlyList<PdoDecodeResult> Options { get; init; } = Array.Empty<PdoDecodeResult>();
    [JsonPropertyName("negotiated")] public PdoDecodeResult? Negotiated { get; init; }
}

public sealed record Snapshot
{
    [JsonPropertyName("version"), JsonPropertyOrder(0)] public string Version { get; init; } = string.Empty;
    [JsonPropertyName("isDesktopMac"), JsonPropertyOrder(1)] public bool IsDesktopMac { get; init; }
    [JsonPropertyName("adapter"), JsonPropertyOrder(2)] public AdapterSnapshot? Adapter { get; init; }
    [JsonPropertyName("ports"), JsonPropertyOrder(3)] public IReadOnlyList<Port> Ports { get; init; } = Array.Empty<Port>();
    [JsonPropertyName("thunderboltSwitches"), JsonPropertyOrder(4)] public IReadOnlyList<object> ThunderboltSwitches { get; init; } = Array.Empty<object>();
}

public sealed record AdapterSnapshot
{
    [JsonPropertyName("watts")] public int? Watts { get; init; }
    [JsonPropertyName("source")] public string? Source { get; init; }
    [JsonPropertyName("voltageMV")] public int? VoltageMV { get; init; }
    [JsonPropertyName("currentMA")] public int? CurrentMA { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("powerTier")] public int? PowerTier { get; init; }
    [JsonPropertyName("isWireless")] public bool? IsWireless { get; init; }
}
