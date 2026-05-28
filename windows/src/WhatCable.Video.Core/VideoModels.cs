using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WhatCable.Video.Core;

/// <summary>
/// Connector type for a video port.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VideoConnectorType
{
    Unknown,
    HDMI,
    DisplayPort,
    UsbCDisplayPort,
    DVI,
    VGA,
    Thunderbolt
}

/// <summary>
/// Video mode resolution and refresh rate.
/// </summary>
public sealed record VideoMode
{
    [JsonPropertyName("widthPx")] public int WidthPx { get; init; }
    [JsonPropertyName("heightPx")] public int HeightPx { get; init; }
    [JsonPropertyName("refreshRateHz")] public double RefreshRateHz { get; init; }
    [JsonPropertyName("interlaced")] public bool Interlaced { get; init; }
    [JsonPropertyName("pixelClockMhz")] public double? PixelClockMhz { get; init; }
}

/// <summary>
/// Cable class for video cables (HDMI/DP/USB-C).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VideoCableClass
{
    Unknown,
    HdmiStandard,     // HDMI 1.4 (up to 4K30 or 1080p120)
    HdmiHighSpeed,    // HDMI 2.0 (up to 4K60)
    HdmiUltraHighSpeed, // HDMI 2.1 (up to 8K60 or 4K120+)
    DisplayPort14,    // DP 1.4 (HBR3, up to 8.1 Gbps per lane)
    DisplayPort20Uhbr10, // DP 2.0 UHBR10 (10 Gbps per lane)
    DisplayPort20Uhbr135, // DP 2.0 UHBR13.5 (13.5 Gbps per lane)
    DisplayPort20Uhbr20,  // DP 2.0 UHBR20 (20 Gbps per lane)
    UsbC31Gen1,       // USB-C 3.1 Gen1 (DP Alt Mode up to HBR2)
    UsbC31Gen2,       // USB-C 3.1 Gen2 (DP Alt Mode up to HBR3)
    UsbC4Gen3         // USB-C 4 Gen3 (DP Alt Mode up to UHBR20)
}

/// <summary>
/// GPU capabilities for video output.
/// </summary>
public sealed record GpuCapabilities
{
    [JsonPropertyName("vendor")] public string? Vendor { get; init; }
    [JsonPropertyName("model")] public string? Model { get; init; }
    [JsonPropertyName("maxHdmiGen")] public string? MaxHdmiGen { get; init; }
    [JsonPropertyName("maxDisplayPortGen")] public string? MaxDisplayPortGen { get; init; }
    [JsonPropertyName("maxPixelClockMhz")] public double? MaxPixelClockMhz { get; init; }
}

/// <summary>
/// Snapshot of a video port's current state and capabilities.
/// </summary>
public sealed record VideoPortSnapshot
{
    [JsonPropertyName("connectorType")] public VideoConnectorType ConnectorType { get; init; }
    [JsonPropertyName("activeMode")] public VideoMode? ActiveMode { get; init; }
    [JsonPropertyName("sinkMaxMode")] public VideoMode? SinkMaxMode { get; init; }
    [JsonPropertyName("advertisedCableClass")] public VideoCableClass? AdvertisedCableClass { get; init; }
    [JsonPropertyName("sourceGpuCaps")] public GpuCapabilities? SourceGpuCaps { get; init; }
    [JsonIgnore] public byte[]? EdidRaw { get; init; }
    [JsonPropertyName("edidParsed")] public EdidInfo? EdidParsed { get; init; }

    /// <summary>
    /// Live source-side link state reported by an optional vendor GPU SDK
    /// (NVAPI / AMD ADL/ADLX / Intel IGCL). Additive and <c>null</c> on a stock build with
    /// no vendor SDK present; when populated it lets the diagnostic distinguish a
    /// cable-limited link from an intentional mode selection with higher confidence.
    /// </summary>
    [JsonPropertyName("vendorGpuLink")] public VendorGpuLinkInfo? VendorGpuLink { get; init; }
}

/// <summary>
/// Source-side video link state read from a vendor GPU SDK. All fields are optional because
/// each vendor exposes a different subset (NVAPI reports DP link rate/lane count, HDMI FRL
/// rate, scrambling and DSC state; AMD ADL/ADLX reports the DisplayPort link config and HDMI
/// signal info; Intel IGCL reports DP/HDMI link settings via <c>ctl_get_display_*</c>).
/// </summary>
public sealed record VendorGpuLinkInfo
{
    // Most DisplayPort and HDMI FRL links use the full four-lane configuration; assumed when
    // the vendor SDK reports a per-lane rate but no explicit lane count.
    private const int DefaultLaneCount = 4;

    /// <summary>Vendor SDK that produced this data (e.g. <c>NVIDIA</c>, <c>AMD</c>, <c>Intel</c>).</summary>
    [JsonPropertyName("vendor")] public string? Vendor { get; init; }

    /// <summary>Negotiated DisplayPort link rate per lane, in Gbps (e.g. 8.1 for HBR3, 20 for UHBR20).</summary>
    [JsonPropertyName("dpLinkRateGbps")] public double? DpLinkRateGbps { get; init; }

    /// <summary>Negotiated DisplayPort lane count (typically 1, 2 or 4).</summary>
    [JsonPropertyName("dpLaneCount")] public int? DpLaneCount { get; init; }

    /// <summary>Negotiated HDMI Fixed Rate Link rate per lane, in Gbps (e.g. 3, 6, 8, 10, 12).</summary>
    [JsonPropertyName("hdmiFrlRateGbps")] public double? HdmiFrlRateGbps { get; init; }

    /// <summary>Number of HDMI FRL lanes in use (3 or 4); <c>null</c> when running in legacy TMDS mode.</summary>
    [JsonPropertyName("hdmiFrlLaneCount")] public int? HdmiFrlLaneCount { get; init; }

    /// <summary>True when link scrambling is enabled (HDMI 2.1 FRL / high TMDS rates).</summary>
    [JsonPropertyName("scramblingEnabled")] public bool? ScramblingEnabled { get; init; }

    /// <summary>True when Display Stream Compression is active on the link.</summary>
    [JsonPropertyName("dscEnabled")] public bool? DscEnabled { get; init; }

    /// <summary>
    /// Total raw negotiated link bandwidth across all lanes, in Gbps, when reported directly
    /// by the vendor SDK. When <c>null</c> it is derived from the DP / HDMI FRL fields via
    /// <see cref="TotalLinkBandwidthGbps"/>.
    /// </summary>
    [JsonPropertyName("negotiatedLinkRateGbps")] public double? NegotiatedLinkRateGbps { get; init; }

    /// <summary>
    /// Total raw negotiated link bandwidth in Gbps: the explicit
    /// <see cref="NegotiatedLinkRateGbps"/> when present, otherwise <c>rate × lanes</c>
    /// derived from the DisplayPort or HDMI FRL fields. Returns <c>null</c> when nothing is known.
    /// </summary>
    [JsonIgnore]
    public double? TotalLinkBandwidthGbps
    {
        get
        {
            if (NegotiatedLinkRateGbps is { } negotiated and > 0)
            {
                return negotiated;
            }

            if (DpLinkRateGbps is { } dpRate and > 0)
            {
                int lanes = DpLaneCount is { } dpLanes and > 0 ? dpLanes : DefaultLaneCount;
                return dpRate * lanes;
            }

            if (HdmiFrlRateGbps is { } frlRate and > 0)
            {
                int lanes = HdmiFrlLaneCount is { } frlLanes and > 0 ? frlLanes : DefaultLaneCount;
                return frlRate * lanes;
            }

            return null;
        }
    }
}

/// <summary>
/// A single display port paired with its link diagnostic, as emitted in the CLI
/// <c>video</c> section. The snapshot is produced from <c>QueryDisplayConfig</c> +
/// the EDID, and the diagnostic from <see cref="VideoLinkDiagnostic.Analyze"/>.
/// </summary>
public sealed record VideoPortReport
{
    [JsonPropertyName("displayName")] public string? DisplayName { get; init; }
    [JsonPropertyName("port")] public VideoPortSnapshot Port { get; init; } = new();
    [JsonPropertyName("diagnostic")] public VideoLinkDiagnosticResult Diagnostic { get; init; } = new();
}

/// <summary>
/// Bottleneck classification for video link diagnostic.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VideoBottleneck
{
    None,           // No bottleneck, optimal configuration
    Source,         // GPU/source is the limiting factor
    Sink,           // Display/sink is the limiting factor
    Cable,          // Cable is the limiting factor
    Unknown         // Cannot determine
}

/// <summary>
/// Video link diagnostic result with bottleneck classification and human-readable verdict.
/// </summary>
public sealed record VideoLinkDiagnosticResult
{
    [JsonPropertyName("bottleneck")] public VideoBottleneck Bottleneck { get; init; }
    [JsonPropertyName("verdict")] public string Verdict { get; init; } = string.Empty;
    [JsonPropertyName("details")] public IReadOnlyList<string> Details { get; init; } = Array.Empty<string>();
    [JsonPropertyName("recommendedCableClass")] public VideoCableClass? RecommendedCableClass { get; init; }
}

/// <summary>
/// Parsed EDID information.
/// </summary>
public sealed record EdidInfo
{
    [JsonPropertyName("version")] public string Version { get; init; } = string.Empty;
    [JsonPropertyName("manufacturer")] public string? Manufacturer { get; init; }
    [JsonPropertyName("productCode")] public int ProductCode { get; init; }
    [JsonPropertyName("serialNumber")] public uint SerialNumber { get; init; }
    [JsonPropertyName("manufactureWeek")] public int? ManufactureWeek { get; init; }
    [JsonPropertyName("manufactureYear")] public int? ManufactureYear { get; init; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; init; }
    [JsonPropertyName("widthCm")] public int? WidthCm { get; init; }
    [JsonPropertyName("heightCm")] public int? HeightCm { get; init; }
    [JsonPropertyName("nativeResolution")] public VideoMode? NativeResolution { get; init; }
    [JsonPropertyName("maxPixelClockMhz")] public double? MaxPixelClockMhz { get; init; }
    [JsonPropertyName("supportedModes")] public IReadOnlyList<VideoMode> SupportedModes { get; init; } = Array.Empty<VideoMode>();
    [JsonPropertyName("extensionBlocks")] public int ExtensionBlocks { get; init; }
    [JsonPropertyName("ctaInfo")] public CtaInfo? CtaInfo { get; init; }
    [JsonPropertyName("displayIdInfo")] public DisplayIdInfo? DisplayIdInfo { get; init; }
}

/// <summary>
/// CTA-861 extension block information.
/// </summary>
public sealed record CtaInfo
{
    [JsonPropertyName("revision")] public int Revision { get; init; }
    [JsonPropertyName("underscanSupport")] public bool UnderscanSupport { get; init; }
    [JsonPropertyName("basicAudioSupport")] public bool BasicAudioSupport { get; init; }
    [JsonPropertyName("ycbcr444Support")] public bool Ycbcr444Support { get; init; }
    [JsonPropertyName("ycbcr422Support")] public bool Ycbcr422Support { get; init; }
    [JsonPropertyName("videoDescriptors")] public IReadOnlyList<VideoDescriptor> VideoDescriptors { get; init; } = Array.Empty<VideoDescriptor>();
    [JsonPropertyName("audioDescriptors")] public IReadOnlyList<AudioDescriptor> AudioDescriptors { get; init; } = Array.Empty<AudioDescriptor>();
    [JsonPropertyName("hdmiVsdb")] public HdmiVsdb? HdmiVsdb { get; init; }
    [JsonPropertyName("hdmiForumVsdb")] public HdmiForumVsdb? HdmiForumVsdb { get; init; }
    [JsonPropertyName("hdrStaticMetadata")] public HdrStaticMetadata? HdrStaticMetadata { get; init; }
    [JsonPropertyName("speakerAllocation")] public SpeakerAllocation? SpeakerAllocation { get; init; }
    [JsonPropertyName("colorimetry")] public ColorimetryDataBlock? Colorimetry { get; init; }
    [JsonPropertyName("videoCapability")] public VideoCapabilityDataBlock? VideoCapability { get; init; }
}

/// <summary>
/// Short Video Descriptor (VIC code).
/// </summary>
public sealed record VideoDescriptor
{
    [JsonPropertyName("vic")] public int Vic { get; init; }
    [JsonPropertyName("native")] public bool Native { get; init; }
    [JsonPropertyName("mode")] public VideoMode? Mode { get; init; }
}

/// <summary>
/// Short Audio Descriptor.
/// </summary>
public sealed record AudioDescriptor
{
    [JsonPropertyName("format")] public string Format { get; init; } = string.Empty;
    [JsonPropertyName("maxChannels")] public int MaxChannels { get; init; }
    [JsonPropertyName("sampleRatesKhz")] public IReadOnlyList<int> SampleRatesKhz { get; init; } = Array.Empty<int>();
    [JsonPropertyName("bitDepths")] public IReadOnlyList<int>? BitDepths { get; init; }
    [JsonPropertyName("maxBitrateKbps")] public int? MaxBitrateKbps { get; init; }
}

/// <summary>
/// HDMI Vendor Specific Data Block (HDMI 1.4).
/// </summary>
public sealed record HdmiVsdb
{
    [JsonPropertyName("sourcePhysicalAddress")] public string? SourcePhysicalAddress { get; init; }
    [JsonPropertyName("supports30bitDeepColor")] public bool Supports30bitDeepColor { get; init; }
    [JsonPropertyName("supports36bitDeepColor")] public bool Supports36bitDeepColor { get; init; }
    [JsonPropertyName("supports48bitDeepColor")] public bool Supports48bitDeepColor { get; init; }
    [JsonPropertyName("supportsAi")] public bool SupportsAi { get; init; }
    [JsonPropertyName("maxTmdsClockMhz")] public int? MaxTmdsClockMhz { get; init; }
}

/// <summary>
/// HDMI Forum Vendor Specific Data Block (HDMI 2.1).
/// </summary>
public sealed record HdmiForumVsdb
{
    [JsonPropertyName("version")] public int Version { get; init; }
    [JsonPropertyName("maxFrlRate")] public int? MaxFrlRate { get; init; }
    [JsonPropertyName("supportsDsc")] public bool SupportsDsc { get; init; }
    [JsonPropertyName("supportsAllm")] public bool SupportsAllm { get; init; }
    [JsonPropertyName("supportsVrr")] public bool SupportsVrr { get; init; }
    [JsonPropertyName("supportsQms")] public bool SupportsQms { get; init; }
    [JsonPropertyName("supportsQft")] public bool SupportsQft { get; init; }
    [JsonPropertyName("maxTmdsCharacterRateMhz")] public int? MaxTmdsCharacterRateMhz { get; init; }
}

/// <summary>
/// HDR Static Metadata Data Block.
/// </summary>
public sealed record HdrStaticMetadata
{
    [JsonPropertyName("eotfs")] public IReadOnlyList<string> Eotfs { get; init; } = Array.Empty<string>();
    [JsonPropertyName("staticMetadataTypes")] public IReadOnlyList<int> StaticMetadataTypes { get; init; } = Array.Empty<int>();
    [JsonPropertyName("maxLuminanceCdM2")] public int? MaxLuminanceCdM2 { get; init; }
    [JsonPropertyName("maxFrameAvgLuminanceCdM2")] public int? MaxFrameAvgLuminanceCdM2 { get; init; }
    [JsonPropertyName("minLuminanceCdM2")] public double? MinLuminanceCdM2 { get; init; }
}

/// <summary>
/// Speaker Allocation Data Block.
/// </summary>
public sealed record SpeakerAllocation
{
    [JsonPropertyName("frontLeftRight")] public bool FrontLeftRight { get; init; }
    [JsonPropertyName("lfe")] public bool Lfe { get; init; }
    [JsonPropertyName("frontCenter")] public bool FrontCenter { get; init; }
    [JsonPropertyName("rearLeftRight")] public bool RearLeftRight { get; init; }
    [JsonPropertyName("rearCenter")] public bool RearCenter { get; init; }
    [JsonPropertyName("frontCenterLeftRight")] public bool FrontCenterLeftRight { get; init; }
    [JsonPropertyName("rearCenterLeftRight")] public bool RearCenterLeftRight { get; init; }
}

/// <summary>
/// Colorimetry Data Block.
/// </summary>
public sealed record ColorimetryDataBlock
{
    [JsonPropertyName("xvycc601")] public bool Xvycc601 { get; init; }
    [JsonPropertyName("xvycc709")] public bool Xvycc709 { get; init; }
    [JsonPropertyName("sycc601")] public bool Sycc601 { get; init; }
    [JsonPropertyName("adobeYcc601")] public bool AdobeYcc601 { get; init; }
    [JsonPropertyName("adobeRgb")] public bool AdobeRgb { get; init; }
    [JsonPropertyName("bt2020Ycc")] public bool Bt2020Ycc { get; init; }
    [JsonPropertyName("bt2020Rgb")] public bool Bt2020Rgb { get; init; }
    [JsonPropertyName("dciP3")] public bool DciP3 { get; init; }
}

/// <summary>
/// Video Capability Data Block.
/// </summary>
public sealed record VideoCapabilityDataBlock
{
    [JsonPropertyName("quantizationRangeSelectable")] public bool QuantizationRangeSelectable { get; init; }
    [JsonPropertyName("preferredTimingMode")] public string? PreferredTimingMode { get; init; }
}

/// <summary>
/// DisplayID 2.0 information.
/// </summary>
public sealed record DisplayIdInfo
{
    [JsonPropertyName("version")] public string Version { get; init; } = string.Empty;
    [JsonPropertyName("primaryUseCase")] public string? PrimaryUseCase { get; init; }
    [JsonPropertyName("maxLinkRate")] public string? MaxLinkRate { get; init; }
    [JsonPropertyName("uhbrSupport")] public IReadOnlyList<string> UhbrSupport { get; init; } = Array.Empty<string>();
    [JsonPropertyName("supportedModes")] public IReadOnlyList<VideoMode> SupportedModes { get; init; } = Array.Empty<VideoMode>();
}

/// <summary>
/// Known video cable entry for certification database.
/// </summary>
public sealed record KnownVideoCable
{
    [JsonPropertyName("certificationId")] public string CertificationId { get; init; } = string.Empty;
    [JsonPropertyName("cableClass")] public VideoCableClass CableClass { get; init; }
    [JsonPropertyName("vendor")] public string? Vendor { get; init; }
    [JsonPropertyName("model")] public string? Model { get; init; }
    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
}
