using System.Text.Json;
using System.Text.Json.Serialization;
using WhatCable.Core;

namespace WhatCable.Windows.Backend.Ucsi;

/// <summary>
/// Drives an <see cref="IUcsiTransport"/> to build the structured <see cref="UcsiSystem"/> and to
/// map each Type-C connector onto the platform-neutral <see cref="Port"/> model used by the CLI/UI.
/// </summary>
/// <remarks>
/// All transport access is defensive: a failure to read or decode any single field is swallowed and
/// surfaced through the per-field <see cref="UcsiFieldAvailability"/> flags rather than throwing, so
/// the CLI always produces a valid (possibly partial) snapshot. UCSI 2.0-only features
/// (GET_PD_MESSAGE, LIQUID_DETECTION) are gated on the reported <see cref="IUcsiTransport.Version"/>.
/// </remarks>
public sealed class UcsiAdapter
{
    /// <summary>UCSI BCD version at which the 2.0-only commands (PD message, liquid detection) appear.</summary>
    public const ushort Ucsi20 = 0x0200;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IUcsiTransport _transport;

    public UcsiAdapter(IUcsiTransport transport)
        => _transport = transport ?? throw new ArgumentNullException(nameof(transport));

    public bool IsAvailable => _transport.IsAvailable;

    /// <summary>Builds the structured UCSI snapshot (version + per-connector detail).</summary>
    public UcsiSystem GetSystem()
    {
        if (!_transport.IsAvailable)
        {
            return new UcsiSystem { Available = false };
        }

        var connectors = new List<UcsiConnector>();
        var count = Math.Max(0, _transport.ConnectorCount);
        for (var i = 1; i <= count; i++)
        {
            connectors.Add(BuildConnector(i));
        }

        return new UcsiSystem
        {
            Available = true,
            VersionBcd = _transport.Version,
            UcsiVersion = _transport.Version == 0 ? null : UcsiDecoder.FormatVersion(_transport.Version),
            Connectors = connectors,
        };
    }

    /// <summary>Maps the UCSI connectors onto schema <see cref="Port"/>s for the snapshot.</summary>
    public IReadOnlyList<Port> GetPorts()
    {
        var system = GetSystem();
        if (!system.Available)
        {
            return Array.Empty<Port>();
        }

        return system.Connectors.Select(ToPort).ToList();
    }

    private UcsiConnector BuildConnector(int index)
    {
        var isUcsi20 = _transport.Version >= Ucsi20;

        var capability = Decode(() => _transport.GetConnectorCapability(index), d => UcsiDecoder.DecodeConnectorCapability(d));
        var status = Decode(() => _transport.GetConnectorStatus(index), d => UcsiDecoder.DecodeConnectorStatus(d));

        var sourcePdos = DecodeList(() => _transport.GetPdos(index, partner: true, source: true), d => UcsiDecoder.DecodePdos(d));
        var sinkPdos = DecodeList(() => _transport.GetPdos(index, partner: true, source: false), d => UcsiDecoder.DecodePdos(d));

        var cableProperty = Decode(() => _transport.GetCableProperty(index), d => UcsiDecoder.DecodeCableProperty(d));

        var alternateModes = DecodeList(
            () => _transport.GetAlternateModes(index, UcsiRecipient.Connector),
            d => UcsiDecoder.DecodeAlternateModes(d));
        alternateModes = ApplyCurrentCam(index, alternateModes);

        // PD message round-trips (UCSI 2.0): Discover Identity to SOP' (cable) and SOP (partner),
        // plus Discover Modes to the partner.
        DiscoverIdentityPayload? cableIdentity = null;
        DiscoverIdentityPayload? partnerIdentity = null;
        DiscoverModesPayload? partnerModes = null;
        CableVdo? cableVdo = null;
        if (isUcsi20)
        {
            cableIdentity = Decode(
                () => _transport.GetPdMessage(index, UcsiRecipient.SopPrime, UcsiPdResponse.DiscoverIdentity),
                bytes => DiscoverPayloadDecoder.DecodeDiscoverIdentity(bytes));
            cableVdo = DecodeCableVdo(cableIdentity);

            partnerIdentity = Decode(
                () => _transport.GetPdMessage(index, UcsiRecipient.Connector, UcsiPdResponse.DiscoverIdentity),
                bytes => DiscoverPayloadDecoder.DecodeDiscoverIdentity(bytes));

            partnerModes = Decode(
                () => _transport.GetPdMessage(index, UcsiRecipient.Connector, UcsiPdResponse.DiscoverModes),
                bytes => DiscoverPayloadDecoder.DecodeDiscoverModes(bytes));
        }

        bool? liquidDetected = null;
        if (isUcsi20)
        {
            try
            {
                liquidDetected = _transport.GetLiquidDetected(index);
            }
            catch
            {
                liquidDetected = null;
            }
        }

        var availability = new UcsiFieldAvailability
        {
            Capability = capability is not null,
            Status = status is not null,
            SourcePdos = sourcePdos.Count > 0,
            SinkPdos = sinkPdos.Count > 0,
            CableProperty = cableProperty is not null,
            CableIdentity = cableIdentity is not null,
            PartnerIdentity = partnerIdentity is not null,
            AlternateModes = alternateModes.Count > 0,
            LiquidDetection = isUcsi20 && liquidDetected is not null,
        };

        return new UcsiConnector
        {
            Index = index,
            VersionBcd = _transport.Version,
            UcsiVersion = _transport.Version == 0 ? null : UcsiDecoder.FormatVersion(_transport.Version),
            Capability = capability,
            Status = status,
            SourcePdos = sourcePdos,
            SinkPdos = sinkPdos,
            CableProperty = cableProperty,
            CableIdentity = cableIdentity,
            CableVdo = cableVdo,
            PartnerIdentity = partnerIdentity,
            PartnerModes = partnerModes,
            AlternateModes = alternateModes,
            LiquidDetected = liquidDetected,
            Available = availability,
        };
    }

    private IReadOnlyList<UcsiAlternateMode> ApplyCurrentCam(int index, IReadOnlyList<UcsiAlternateMode> modes)
    {
        if (modes.Count == 0)
        {
            return modes;
        }

        uint? supported = null;
        try
        {
            var rawSupported = _transport.GetCamSupported(index);
            if (rawSupported is { Length: > 0 })
            {
                supported = UcsiDecoder.DecodeCamSupported(rawSupported);
            }
        }
        catch
        {
            return modes;
        }

        int currentCam;
        try
        {
            var rawCurrent = _transport.GetCurrentCam(index);
            currentCam = rawCurrent is { Length: > 0 } ? UcsiDecoder.DecodeCurrentCam(rawCurrent) : 0;
        }
        catch
        {
            return modes;
        }

        if (currentCam < 1 || currentCam > modes.Count)
        {
            return modes;
        }

        var modeIndex = currentCam - 1;
        if (supported is uint bitmap && modeIndex < 32 && ((bitmap >> modeIndex) & 1) == 0)
        {
            return modes;
        }

        return modes
            .Select((mode, i) => i == modeIndex ? mode with { Active = true } : mode)
            .ToList();
    }

    private static CableVdo? DecodeCableVdo(DiscoverIdentityPayload? identity)
    {
        if (identity?.Header is not { } header || identity.RawVdos.Count <= 3)
        {
            return null;
        }

        var isActive = header.UfpProductType == ProductType.ActiveCable;
        if (!isActive && header.UfpProductType != ProductType.PassiveCable)
        {
            return null;
        }

        return UsbPdDecoder.DecodeCableVdo(identity.RawVdos[3], isActive);
    }

    private Port ToPort(UcsiConnector connector)
    {
        var connected = connector.Status?.Connected ?? false;
        var powerSources = BuildPowerSources(connector);
        var cable = BuildCable(connector);

        var ucsiElement = JsonSerializer.SerializeToElement(connector, SerializerOptions);

        return new Port
        {
            Name = $"USB-C Connector {connector.Index}",
            Type = "USB-C",
            ClassName = "UsbCConnector",
            ConnectionActive = connected,
            PdCapable = true,
            Status = connected ? "connected" : "available",
            Headline = "USB-C connector (UCSI)",
            Subtitle = BuildSubtitle(connector, connected),
            PowerSources = powerSources,
            Cable = cable,
            Ucsi = ucsiElement,
        };
    }

    private static string BuildSubtitle(UcsiConnector connector, bool connected)
    {
        if (!connected)
        {
            return "No device attached";
        }

        var watts = MaxWatts(connector.SourcePdos);
        return watts > 0 ? $"Attached — up to {watts} W" : "Attached";
    }

    private static IReadOnlyList<ChargerProfile> BuildPowerSources(UcsiConnector connector)
    {
        if (connector.SourcePdos.Count == 0)
        {
            return Array.Empty<ChargerProfile>();
        }

        return new[]
        {
            new ChargerProfile
            {
                Name = "USB-C Power Delivery",
                MaxPowerW = MaxWatts(connector.SourcePdos),
                Options = connector.SourcePdos,
                Negotiated = SelectNegotiated(connector),
            },
        };
    }

    private static PdoDecodeResult? SelectNegotiated(UcsiConnector connector)
    {
        if (connector.Status?.RequestDataObject is not { } rdo)
        {
            return null;
        }

        // RDO object position (1-based) lives in bits 30..28.
        var position = (int)((rdo >> 28) & 0x7);
        return position >= 1 && position <= connector.SourcePdos.Count
            ? connector.SourcePdos[position - 1]
            : null;
    }

    private static int MaxWatts(IReadOnlyList<PdoDecodeResult> pdos)
    {
        var maxMw = 0;
        foreach (var pdo in pdos)
        {
            var mw = pdo switch
            {
                { MaxPowerMw: { } p } => p,
                { MaxVoltageMv: { } v, MaxCurrentMa: { } c } => v * c / 1000,
                _ => 0,
            };
            if (mw > maxMw)
            {
                maxMw = mw;
            }
        }

        return maxMw / 1000;
    }

    private static Cable? BuildCable(UcsiConnector connector)
    {
        var cableVdo = connector.CableVdo;
        var property = connector.CableProperty;
        if (cableVdo is null && property is null)
        {
            return null;
        }

        var vendorId = connector.CableIdentity?.Header?.VendorId ?? 0;
        var trustFlags = cableVdo is not null
            ? CableTrustScorer.Evaluate(vendorId, cableVdo)
            : null;

        return new Cable
        {
            Endpoint = "cablePlugSOPPrime",
            VendorId = vendorId,
            VendorName = vendorId == 0 ? null : VendorDatabase.Name(vendorId),
            Speed = DescribeCableSpeed(cableVdo, property),
            CurrentRating = DescribeCurrent(cableVdo, property),
            MaxVolts = cableVdo?.MaxVolts,
            MaxWatts = cableVdo?.MaxWatts,
            Type = DescribeCableType(cableVdo, property),
            TrustFlags = trustFlags is { Count: > 0 } ? trustFlags : null,
        };
    }

    private static string? DescribeCableSpeed(CableVdo? vdo, UcsiCableProperty? property)
        => vdo?.Speed switch
        {
            CableSpeed.Usb20 => "USB 2.0 (480 Mbps)",
            CableSpeed.Usb32Gen1 => "USB 3.2 Gen 1 (5 Gbps)",
            CableSpeed.Usb32Gen2 => "USB 3.2 Gen 2 (10 Gbps)",
            CableSpeed.Usb4Gen3 => "USB4 Gen 3 (40 Gbps)",
            CableSpeed.Usb4Gen4 => "USB4 Gen 4 (80 Gbps)",
            _ => property is { SpeedSupported: > 0 } ? "USB SuperSpeed" : null,
        };

    private static string? DescribeCurrent(CableVdo? vdo, UcsiCableProperty? property)
    {
        if (vdo is not null)
        {
            return vdo.Current switch
            {
                CableCurrent.FiveAmp => "5 A",
                CableCurrent.ThreeAmp => "3 A",
                _ => "USB default",
            };
        }

        return property is { CurrentCapabilityMa: > 0 }
            ? $"{property.CurrentCapabilityMa / 1000.0:0.#} A"
            : null;
    }

    private static string? DescribeCableType(CableVdo? vdo, UcsiCableProperty? property)
    {
        var active = vdo?.CableType == CableType.Active || property?.Active == true;
        if (vdo is null && property is null)
        {
            return null;
        }

        return active ? "Active" : "Passive";
    }

    private static T? Decode<T>(Func<byte[]?> fetch, Func<byte[], T> decode)
        where T : class
    {
        try
        {
            var data = fetch();
            return data is null || data.Length == 0 ? null : decode(data);
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<T> DecodeList<T>(Func<byte[]?> fetch, Func<byte[], IReadOnlyList<T>> decode)
    {
        try
        {
            var data = fetch();
            return data is null || data.Length == 0 ? Array.Empty<T>() : decode(data);
        }
        catch
        {
            return Array.Empty<T>();
        }
    }
}
