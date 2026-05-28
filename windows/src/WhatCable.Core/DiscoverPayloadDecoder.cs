using System.Buffers.Binary;

namespace WhatCable.Core;

public static class DiscoverPayloadDecoder
{
    public static DiscoverIdentityPayload DecodeDiscoverIdentity(ReadOnlySpan<byte> payload)
    {
        var vdos = ReadVdos(payload);
        return new DiscoverIdentityPayload(
            Header: vdos.Count > 0 ? UsbPdDecoder.DecodeIdHeader(vdos[0]) : null,
            CertStat: vdos.Count > 1 ? UsbPdDecoder.DecodeCertStat(vdos[1]) : null,
            Product: vdos.Count > 2 ? UsbPdDecoder.DecodeProduct(vdos[2]) : null,
            ProductTypeVdo1: vdos.Count > 3 ? UsbPdDecoder.DecodeProductTypeVdo1(vdos[3]) : null,
            ProductTypeVdo2: vdos.Count > 4 ? UsbPdDecoder.DecodeProductTypeVdo2(vdos[4]) : null,
            ProductTypeVdo3: vdos.Count > 5 ? UsbPdDecoder.DecodeProductTypeVdo3(vdos[5]) : null,
            RawVdos: vdos);
    }

    public static DiscoverSvidsPayload DecodeDiscoverSvids(ReadOnlySpan<byte> payload)
    {
        var vdos = ReadVdos(payload);
        var svids = new List<int>();
        foreach (var vdo in vdos)
        {
            var low = (int)(vdo & 0xFFFF);
            var high = (int)((vdo >> 16) & 0xFFFF);
            if (low != 0) svids.Add(low);
            if (high != 0) svids.Add(high);
        }
        return new DiscoverSvidsPayload(svids, vdos);
    }

    public static DiscoverModesPayload DecodeDiscoverModes(ReadOnlySpan<byte> payload)
    {
        var vdos = ReadVdos(payload);
        return new DiscoverModesPayload(vdos.Select(v => new ModeVdo(v)).ToArray(), vdos);
    }

    private static List<uint> ReadVdos(ReadOnlySpan<byte> payload)
    {
        var vdos = new List<uint>(payload.Length / 4);
        for (var i = 0; i + 3 < payload.Length; i += 4)
        {
            vdos.Add(BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(i, 4)));
        }

        return vdos;
    }
}

public sealed record DiscoverIdentityPayload(IdHeaderVdo? Header, CertStatVdo? CertStat, ProductVdo? Product, ProductTypeVdo1? ProductTypeVdo1, ProductTypeVdo2? ProductTypeVdo2, ProductTypeVdo3? ProductTypeVdo3, IReadOnlyList<uint> RawVdos);
public sealed record DiscoverSvidsPayload(IReadOnlyList<int> Svids, IReadOnlyList<uint> RawVdos);
public sealed record DiscoverModesPayload(IReadOnlyList<ModeVdo> Modes, IReadOnlyList<uint> RawVdos);
public sealed record ModeVdo(uint Raw);
