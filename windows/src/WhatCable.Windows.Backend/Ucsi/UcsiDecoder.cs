using System.Buffers.Binary;
using WhatCable.Core;

namespace WhatCable.Windows.Backend.Ucsi;

/// <summary>
/// Decodes the raw little-endian DATA buffers returned by <see cref="IUcsiTransport"/> into the
/// strongly-typed UCSI models. PD message payloads (PDOs, Discover Identity / Modes VDOs) are
/// decoded by reusing the <see cref="WhatCable.Core"/> decoders shared with the macOS app.
/// </summary>
public static class UcsiDecoder
{
    /// <summary>Decodes a UCSI BCD version (e.g. 0x0200) into "major.minor".</summary>
    public static string FormatVersion(ushort bcd)
    {
        var major = (bcd >> 8) & 0xFF;
        var minor = (bcd >> 4) & 0x0F;
        return $"{major}.{minor}";
    }

    public static UcsiConnectorCapability DecodeConnectorCapability(ReadOnlySpan<byte> data)
    {
        var reader = new UcsiBitReader(data.ToArray());
        var operationMode = (byte)reader.Read(8);
        var provider = reader.ReadBool();
        var consumer = reader.ReadBool();

        return new UcsiConnectorCapability
        {
            OperationMode = operationMode,
            RpOnly = (operationMode & 0x01) != 0,
            RdOnly = (operationMode & 0x02) != 0,
            Drp = (operationMode & 0x04) != 0,
            AnalogAudio = (operationMode & 0x08) != 0,
            DebugAccessory = (operationMode & 0x10) != 0,
            Usb2 = (operationMode & 0x20) != 0,
            Usb3 = (operationMode & 0x40) != 0,
            AlternateModes = (operationMode & 0x80) != 0,
            Provider = provider,
            Consumer = consumer,
        };
    }

    public static UcsiConnectorStatus DecodeConnectorStatus(ReadOnlySpan<byte> data)
    {
        var reader = new UcsiBitReader(data.ToArray());
        var statusChange = (ushort)reader.Read(16);
        var powerOpMode = reader.Read(3);
        var connected = reader.ReadBool();
        var sourcing = reader.ReadBool();
        reader.Skip(8); // Connector Partner Flags
        var partnerType = reader.Read(3);
        var rdo = reader.Read(32);

        return new UcsiConnectorStatus
        {
            StatusChange = statusChange,
            PowerOperationMode = Enum.IsDefined(typeof(UcsiPowerOperationMode), (int)powerOpMode)
                ? (UcsiPowerOperationMode)(int)powerOpMode
                : UcsiPowerOperationMode.None,
            Connected = connected,
            Sourcing = sourcing,
            PartnerType = Enum.IsDefined(typeof(UcsiPartnerType), (int)partnerType)
                ? (UcsiPartnerType)(int)partnerType
                : UcsiPartnerType.None,
            RequestDataObject = rdo == 0 ? null : rdo,
        };
    }

    public static UcsiCableProperty DecodeCableProperty(ReadOnlySpan<byte> data)
    {
        var reader = new UcsiBitReader(data.ToArray());
        var speed = (ushort)reader.Read(16);
        var current = reader.Read(8);
        var vbus = reader.ReadBool();
        var active = reader.ReadBool();
        var directional = reader.ReadBool();
        var plugEnd = reader.Read(2);
        var modeSupport = reader.ReadBool();
        var pdRevision = reader.Read(2);
        var latency = reader.Read(4);

        return new UcsiCableProperty
        {
            SpeedSupported = speed,
            CurrentCapabilityMa = (int)current * 50,
            VbusInCable = vbus,
            Active = active,
            Directional = directional,
            PlugEndType = (UcsiPlugEndType)(int)plugEnd,
            ModeSupport = modeSupport,
            PdRevision = (int)pdRevision,
            Latency = (int)latency,
        };
    }

    /// <summary>
    /// Decodes GET_ALTERNATE_MODES data: a sequence of 6-byte {SVID (16-bit), VDO (32-bit)} records.
    /// </summary>
    public static IReadOnlyList<UcsiAlternateMode> DecodeAlternateModes(ReadOnlySpan<byte> data)
    {
        var modes = new List<UcsiAlternateMode>();
        for (var i = 0; i + 6 <= data.Length; i += 6)
        {
            var svid = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(i, 2));
            if (svid == 0)
            {
                break;
            }

            var vdo = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(i + 2, 4));
            modes.Add(new UcsiAlternateMode
            {
                Svid = svid,
                Vdo = vdo,
                Name = SvidName(svid),
            });
        }

        return modes;
    }

    /// <summary>Decodes GET_CAM_SUPPORTED data into a bitmap of active alternate-mode indices.</summary>
    public static uint DecodeCamSupported(ReadOnlySpan<byte> data)
    {
        uint bitmap = 0;
        for (var i = 0; i < data.Length && i < 4; i++)
        {
            bitmap |= (uint)data[i] << (i * 8);
        }

        return bitmap;
    }

    /// <summary>Decodes GET_PDOS data into the platform-neutral PDO list, skipping zero padding.</summary>
    public static IReadOnlyList<PdoDecodeResult> DecodePdos(ReadOnlySpan<byte> data)
    {
        var pdos = new List<PdoDecodeResult>();
        for (var i = 0; i + 4 <= data.Length; i += 4)
        {
            var raw = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(i, 4));
            if (raw == 0)
            {
                continue;
            }

            pdos.Add(PdoDecoder.Decode(raw));
        }

        return pdos;
    }

    /// <summary>Friendly names for well-known Standard / Vendor IDs.</summary>
    public static string? SvidName(int svid) => svid switch
    {
        0xFF01 => "DisplayPort",
        0x8087 => "Thunderbolt",
        0x05AC => "Apple",
        _ => null,
    };
}
