using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatCable.Core;

public sealed record Port
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("className")] public string ClassName { get; init; } = string.Empty;
    [JsonPropertyName("connectionActive")] public bool ConnectionActive { get; init; }
    [JsonPropertyName("pdCapable")] public bool PdCapable { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("headline")] public string Headline { get; init; } = string.Empty;
    [JsonPropertyName("subtitle")] public string Subtitle { get; init; } = string.Empty;
    [JsonPropertyName("bullets")] public IReadOnlyList<string> Bullets { get; init; } = Array.Empty<string>();
    [JsonPropertyName("transports")] public JsonElement? Transports { get; init; }
    [JsonPropertyName("powerSources")] public IReadOnlyList<ChargerProfile> PowerSources { get; init; } = Array.Empty<ChargerProfile>();
    [JsonPropertyName("cable")] public Cable? Cable { get; init; }
    [JsonPropertyName("device")] public DeviceNode? Device { get; init; }
    [JsonPropertyName("charging")] public JsonElement? Charging { get; init; }
    [JsonPropertyName("dataLink")] public JsonElement? DataLink { get; init; }
    [JsonPropertyName("thunderboltSwitchUID")] public long? ThunderboltSwitchUid { get; init; }
    [JsonPropertyName("trm")] public JsonElement? Trm { get; init; }
    [JsonPropertyName("cio")] public JsonElement? Cio { get; init; }
    [JsonPropertyName("devices")] public JsonElement? Devices { get; init; }
    [JsonPropertyName("rawProperties")] public JsonElement? RawProperties { get; init; }
    [JsonPropertyName("ucsi")] public JsonElement? Ucsi { get; init; }
    [JsonPropertyName("unavailable_reason")] public string? UnavailableReason { get; init; }
}

public sealed record Cable
{
    [JsonPropertyName("endpoint")] public string Endpoint { get; init; } = string.Empty;
    [JsonPropertyName("vendorID")] public int VendorId { get; init; }
    [JsonPropertyName("vendorName")] public string? VendorName { get; init; }
    [JsonPropertyName("curatedBrand")] public string? CuratedBrand { get; init; }
    [JsonPropertyName("speed")] public string? Speed { get; init; }
    [JsonPropertyName("currentRating")] public string? CurrentRating { get; init; }
    [JsonPropertyName("maxVolts")] public int? MaxVolts { get; init; }
    [JsonPropertyName("maxWatts")] public int? MaxWatts { get; init; }
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("trustFlags")] public IReadOnlyList<TrustFlag>? TrustFlags { get; init; }
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
    [JsonPropertyName("version")] public string Version { get; init; } = string.Empty;
    [JsonPropertyName("isDesktopMac")] public bool IsDesktopMac { get; init; }
    [JsonPropertyName("adapter")] public AdapterSnapshot? Adapter { get; init; }
    [JsonPropertyName("ports")] public IReadOnlyList<Port> Ports { get; init; } = Array.Empty<Port>();
    [JsonPropertyName("thunderboltSwitches")] public IReadOnlyList<object> ThunderboltSwitches { get; init; } = Array.Empty<object>();
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
