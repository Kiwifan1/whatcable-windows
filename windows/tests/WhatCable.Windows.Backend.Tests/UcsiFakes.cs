using System.Buffers.Binary;
using System.Collections.Generic;
using WhatCable.Windows.Backend.Ucsi;

namespace WhatCable.Windows.Backend.Tests;

/// <summary>
/// In-memory <see cref="IUcsiTransport"/> backed by captured byte streams, keyed by command.
/// Mirrors the byte layout a real Intel / AMD UCSI PC returns so the decode / gating logic can be
/// exercised off-Windows.
/// </summary>
internal sealed class FakeUcsiTransport : IUcsiTransport
{
    private readonly Dictionary<string, byte[]> _streams = new();

    public bool IsAvailable { get; set; } = true;
    public ushort Version { get; set; } = 0x0200;
    public int ConnectorCount { get; set; } = 1;
    public bool? LiquidDetected { get; set; }

    public FakeUcsiTransport Set(string key, byte[] data)
    {
        _streams[key] = data;
        return this;
    }

    private byte[]? Get(string key) => _streams.TryGetValue(key, out var v) ? v : null;

    public byte[]? GetConnectorCapability(int connector) => Get($"cap:{connector}");
    public byte[]? GetConnectorStatus(int connector) => Get($"status:{connector}");
    public byte[]? GetPdos(int connector, bool partner, bool source)
        => Get($"pdos:{connector}:{(partner ? "p" : "l")}:{(source ? "src" : "snk")}");
    public byte[]? GetCableProperty(int connector) => Get($"cable:{connector}");
    public byte[]? GetAlternateModes(int connector, UcsiRecipient recipient) => Get($"altmodes:{connector}:{recipient}");
    public byte[]? GetCamSupported(int connector) => Get($"cam:{connector}");
    public byte[]? GetPdMessage(int connector, UcsiRecipient recipient, UcsiPdResponse response)
        => Get($"pd:{connector}:{recipient}:{response}");
    public bool? GetLiquidDetected(int connector) => LiquidDetected;
}

/// <summary>Packs bitfields LSB-first into a byte buffer, matching <c>UcsiBitReader</c>.</summary>
internal sealed class UcsiBitPacker
{
    private readonly List<byte> _bytes = new();
    private int _bitPos;

    public UcsiBitPacker Write(uint value, int bits)
    {
        for (var i = 0; i < bits; i++)
        {
            var bitIndex = _bitPos + i;
            var byteIndex = bitIndex >> 3;
            while (_bytes.Count <= byteIndex)
            {
                _bytes.Add(0);
            }

            if (((value >> i) & 1) != 0)
            {
                _bytes[byteIndex] |= (byte)(1 << (bitIndex & 7));
            }
        }

        _bitPos += bits;
        return this;
    }

    public byte[] ToArray()
    {
        // Round up to a whole byte.
        var byteLen = (_bitPos + 7) / 8;
        while (_bytes.Count < byteLen)
        {
            _bytes.Add(0);
        }

        return _bytes.ToArray();
    }
}

/// <summary>Little-endian helpers for building PDO / VDO / alt-mode captures.</summary>
internal static class UcsiTestBytes
{
    public static byte[] Words(params uint[] words)
    {
        var buffer = new byte[words.Length * 4];
        for (var i = 0; i < words.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(i * 4, 4), words[i]);
        }

        return buffer;
    }

    public static byte[] AlternateModes(params (int svid, uint vdo)[] modes)
    {
        var buffer = new byte[modes.Length * 6];
        for (var i = 0; i < modes.Length; i++)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(i * 6, 2), (ushort)modes[i].svid);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(i * 6 + 2, 4), modes[i].vdo);
        }

        return buffer;
    }
}
