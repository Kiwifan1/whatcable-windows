using System.Text.Json.Serialization;
using WhatCable.Core;

namespace WhatCable.Windows.Backend.Ucsi;

/// <summary>Decoded GET_CONNECTOR_CAPABILITY data.</summary>
public sealed record UcsiConnectorCapability
{
    [JsonPropertyName("operationMode")] public byte OperationMode { get; init; }
    [JsonPropertyName("rpOnly")] public bool RpOnly { get; init; }
    [JsonPropertyName("rdOnly")] public bool RdOnly { get; init; }
    [JsonPropertyName("drp")] public bool Drp { get; init; }
    [JsonPropertyName("analogAudio")] public bool AnalogAudio { get; init; }
    [JsonPropertyName("debugAccessory")] public bool DebugAccessory { get; init; }
    [JsonPropertyName("usb2")] public bool Usb2 { get; init; }
    [JsonPropertyName("usb3")] public bool Usb3 { get; init; }
    [JsonPropertyName("alternateModes")] public bool AlternateModes { get; init; }
    [JsonPropertyName("provider")] public bool Provider { get; init; }
    [JsonPropertyName("consumer")] public bool Consumer { get; init; }
}

/// <summary>Power operation mode reported in GET_CONNECTOR_STATUS.</summary>
public enum UcsiPowerOperationMode
{
    None = 0,
    UsbDefault = 1,
    BatteryCharging = 2,
    PowerDelivery = 3,
    TypeC1_5A = 4,
    TypeC3A = 5,
}

/// <summary>Connector partner type reported in GET_CONNECTOR_STATUS.</summary>
public enum UcsiPartnerType
{
    None = 0,
    Dfp = 1,
    Ufp = 2,
    CableNoUfp = 3,
    CableWithUfp = 4,
    DebugAccessory = 5,
    AudioAccessory = 6,
}

/// <summary>Decoded GET_CONNECTOR_STATUS data.</summary>
public sealed record UcsiConnectorStatus
{
    [JsonPropertyName("connected")] public bool Connected { get; init; }

    /// <summary>True when sourcing (provider); false when sinking (consumer).</summary>
    [JsonPropertyName("sourcing")] public bool Sourcing { get; init; }

    [JsonPropertyName("powerOperationMode")] public UcsiPowerOperationMode PowerOperationMode { get; init; }
    [JsonPropertyName("partnerType")] public UcsiPartnerType PartnerType { get; init; }

    /// <summary>Raw Request Data Object (the negotiated PD contract), or null when not in a PD contract.</summary>
    [JsonPropertyName("requestDataObject")] public uint? RequestDataObject { get; init; }

    /// <summary>Raw 16-bit connector status-change bitmap.</summary>
    [JsonPropertyName("statusChange")] public ushort StatusChange { get; init; }
}

/// <summary>Cable plug end type reported in GET_CABLE_PROPERTY.</summary>
public enum UcsiPlugEndType
{
    TypeA = 0,
    TypeB = 1,
    TypeC = 2,
    Other = 3,
}

/// <summary>Decoded GET_CABLE_PROPERTY data.</summary>
public sealed record UcsiCableProperty
{
    /// <summary>16-bit bmSpeedSupported field (USB SuperSpeed signaling bitmap).</summary>
    [JsonPropertyName("speedSupported")] public ushort SpeedSupported { get; init; }

    /// <summary>bCurrentCapability in 50 mA units, expressed here in milliamps.</summary>
    [JsonPropertyName("currentCapabilityMa")] public int CurrentCapabilityMa { get; init; }

    [JsonPropertyName("vbusInCable")] public bool VbusInCable { get; init; }

    /// <summary>True for an active cable; false for a passive cable.</summary>
    [JsonPropertyName("active")] public bool Active { get; init; }

    [JsonPropertyName("directional")] public bool Directional { get; init; }
    [JsonPropertyName("plugEndType")] public UcsiPlugEndType PlugEndType { get; init; }
    [JsonPropertyName("modeSupport")] public bool ModeSupport { get; init; }

    /// <summary>Cable PD major revision (UCSI 2.0): 1 = PD2.0, 2 = PD3.0, 3 = PD3.1.</summary>
    [JsonPropertyName("pdRevision")] public int PdRevision { get; init; }

    [JsonPropertyName("latency")] public int Latency { get; init; }
}

/// <summary>A single alternate mode from GET_ALTERNATE_MODES.</summary>
public sealed record UcsiAlternateMode
{
    [JsonPropertyName("svid")] public int Svid { get; init; }
    [JsonPropertyName("vdo")] public uint Vdo { get; init; }

    /// <summary>Friendly name for well-known SVIDs (DisplayPort, Thunderbolt, …).</summary>
    [JsonPropertyName("name")] public string? Name { get; init; }

    /// <summary>True when this mode is currently entered (per GET_CAM_SUPPORTED).</summary>
    [JsonPropertyName("active")] public bool Active { get; init; }
}

/// <summary>Per-field availability flags so the UI / CLI can degrade gracefully.</summary>
public sealed record UcsiFieldAvailability
{
    [JsonPropertyName("capability")] public bool Capability { get; init; }
    [JsonPropertyName("status")] public bool Status { get; init; }
    [JsonPropertyName("sourcePdos")] public bool SourcePdos { get; init; }
    [JsonPropertyName("sinkPdos")] public bool SinkPdos { get; init; }
    [JsonPropertyName("cableProperty")] public bool CableProperty { get; init; }
    [JsonPropertyName("cableIdentity")] public bool CableIdentity { get; init; }
    [JsonPropertyName("partnerIdentity")] public bool PartnerIdentity { get; init; }
    [JsonPropertyName("alternateModes")] public bool AlternateModes { get; init; }
    [JsonPropertyName("liquidDetection")] public bool LiquidDetection { get; init; }
}

/// <summary>Everything UCSI surfaces for one Type-C connector.</summary>
public sealed record UcsiConnector
{
    /// <summary>1-based connector index.</summary>
    [JsonPropertyName("index")] public int Index { get; init; }

    /// <summary>UCSI version BCD (e.g. 0x0200), echoed per connector so each port self-describes.</summary>
    [JsonPropertyName("versionBcd")] public ushort VersionBcd { get; init; }

    /// <summary>Human-readable UCSI version, e.g. "2.0".</summary>
    [JsonPropertyName("ucsi_version")] public string? UcsiVersion { get; init; }

    [JsonPropertyName("capability")] public UcsiConnectorCapability? Capability { get; init; }
    [JsonPropertyName("status")] public UcsiConnectorStatus? Status { get; init; }
    [JsonPropertyName("sourcePdos")] public IReadOnlyList<PdoDecodeResult> SourcePdos { get; init; } = Array.Empty<PdoDecodeResult>();
    [JsonPropertyName("sinkPdos")] public IReadOnlyList<PdoDecodeResult> SinkPdos { get; init; } = Array.Empty<PdoDecodeResult>();
    [JsonPropertyName("cableProperty")] public UcsiCableProperty? CableProperty { get; init; }

    /// <summary>Discover Identity to the cable plug (SOP').</summary>
    [JsonPropertyName("cableIdentity")] public DiscoverIdentityPayload? CableIdentity { get; init; }

    /// <summary>Cable e-marker decoded from the SOP' product-type VDO, if present.</summary>
    [JsonPropertyName("cableVdo")] public CableVdo? CableVdo { get; init; }

    /// <summary>Discover Identity to the port partner (SOP).</summary>
    [JsonPropertyName("partnerIdentity")] public DiscoverIdentityPayload? PartnerIdentity { get; init; }

    /// <summary>Discover Modes to the port partner (SOP).</summary>
    [JsonPropertyName("partnerModes")] public DiscoverModesPayload? PartnerModes { get; init; }

    [JsonPropertyName("alternateModes")] public IReadOnlyList<UcsiAlternateMode> AlternateModes { get; init; } = Array.Empty<UcsiAlternateMode>();

    /// <summary>UCSI 2.0 liquid-detection state, or null when unsupported.</summary>
    [JsonPropertyName("liquidDetected")] public bool? LiquidDetected { get; init; }

    [JsonPropertyName("available")] public UcsiFieldAvailability Available { get; init; } = new();
}

/// <summary>Top-level UCSI snapshot: version plus the per-connector detail.</summary>
public sealed record UcsiSystem
{
    [JsonPropertyName("available")] public bool Available { get; init; }

    /// <summary>UCSI version BCD (e.g. 0x0200).</summary>
    [JsonPropertyName("versionBcd")] public ushort VersionBcd { get; init; }

    /// <summary>Human-readable UCSI version, e.g. "2.0".</summary>
    [JsonPropertyName("ucsi_version")] public string? UcsiVersion { get; init; }

    [JsonPropertyName("connectors")] public IReadOnlyList<UcsiConnector> Connectors { get; init; } = Array.Empty<UcsiConnector>();
}
