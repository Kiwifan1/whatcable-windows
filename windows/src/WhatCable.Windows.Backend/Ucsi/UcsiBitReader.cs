namespace WhatCable.Windows.Backend.Ucsi;

/// <summary>
/// Reads little-endian, LSB-first packed bitfields out of a UCSI MESSAGE_IN DATA buffer.
/// </summary>
/// <remarks>
/// UCSI data structures are defined as bitfields packed starting from the least-significant bit of
/// byte 0. This reader advances a bit cursor and returns the requested number of bits as an
/// unsigned value, clamping to the available data so a truncated capture never throws.
/// </remarks>
internal struct UcsiBitReader
{
    private readonly ReadOnlyMemory<byte> _data;
    private int _bitPos;

    public UcsiBitReader(ReadOnlyMemory<byte> data)
    {
        _data = data;
        _bitPos = 0;
    }

    /// <summary>Total number of bits available in the buffer.</summary>
    public readonly int BitLength => _data.Length * 8;

    /// <summary>Number of bits still unread.</summary>
    public readonly int BitsRemaining => BitLength - _bitPos;

    /// <summary>Reads up to 32 bits and advances the cursor.</summary>
    public uint Read(int bits)
    {
        if (bits is <= 0 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(bits), bits, "Bit count must be between 1 and 32.");
        }

        var span = _data.Span;
        uint value = 0;
        for (var i = 0; i < bits; i++)
        {
            var bitIndex = _bitPos + i;
            var byteIndex = bitIndex >> 3;
            if (byteIndex >= span.Length)
            {
                break; // Past the end of the (possibly truncated) capture: remaining bits read as 0.
            }

            var bit = (span[byteIndex] >> (bitIndex & 7)) & 1;
            value |= (uint)bit << i;
        }

        _bitPos += bits;
        return value;
    }

    /// <summary>Reads a single bit as a boolean.</summary>
    public bool ReadBool() => Read(1) != 0;

    /// <summary>Skips the given number of bits.</summary>
    public void Skip(int bits) => _bitPos += bits;
}
