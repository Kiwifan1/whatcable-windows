namespace WhatCable.Core;

public static class PdoDecoder
{
    public static PdoDecodeResult Decode(uint raw)
    {
        var pdoType = (int)((raw >> 30) & 0b11);
        if (pdoType == 0b11)
        {
            var apdoType = (int)((raw >> 28) & 0b11);
            return apdoType switch
            {
                0b00 => DecodePps(raw),
                0b01 => DecodeEprAvs(raw),
                _ => new PdoDecodeResult("Augmented", raw, null, null, null, null, null, null, null, null, null)
            };
        }

        return pdoType switch
        {
            0b00 => DecodeFixed(raw),
            0b01 => DecodeBattery(raw),
            0b10 => DecodeVariable(raw),
            _ => new PdoDecodeResult("Unknown", raw, null, null, null, null, null, null, null, null, null)
        };
    }

    private static PdoDecodeResult DecodeFixed(uint raw)
    {
        var voltageMv = (int)((raw >> 10) & 0x3FF) * 50;
        var currentMa = (int)(raw & 0x3FF) * 10;
        return new(
            "Fixed",
            raw,
            voltageMv,
            voltageMv,
            currentMa,
            null,
            ((raw >> 29) & 0b1) != 0,
            ((raw >> 28) & 0b1) != 0,
            ((raw >> 27) & 0b1) != 0,
            DecodePeakCurrent(raw),
            null);
    }

    private static PdoDecodeResult DecodeVariable(uint raw)
    {
        var minMv = (int)(raw & 0x3FF) * 50;
        var maxMv = (int)((raw >> 10) & 0x3FF) * 50;
        var currentMa = (int)((raw >> 20) & 0x3FF) * 10;
        return new("Variable", raw, minMv, maxMv, currentMa, null, false, false, false, null, null);
    }

    private static PdoDecodeResult DecodeBattery(uint raw)
    {
        var minMv = (int)(raw & 0x3FF) * 50;
        var maxMv = (int)((raw >> 10) & 0x3FF) * 50;
        var maxMw = (int)((raw >> 20) & 0x3FF) * 250;
        return new("Battery", raw, minMv, maxMv, null, maxMw, false, false, false, null, null);
    }

    private static PdoDecodeResult DecodePps(uint raw)
    {
        var minMv = (int)(raw & 0xFF) * 100;
        var maxMv = (int)((raw >> 8) & 0xFF) * 100;
        var maxMa = (int)((raw >> 17) & 0x7F) * 50;
        return new("PpsApdo", raw, minMv, maxMv, maxMa, null, false, false, false, null, null);
    }

    private static PdoDecodeResult DecodeEprAvs(uint raw)
    {
        var minMv = (int)(raw & 0xFF) * 1000;
        var maxMv = (int)((raw >> 8) & 0xFF) * 1000;
        var maxW = (int)((raw >> 17) & 0x1FF);
        return new("EprAvs", raw, minMv, maxMv, null, maxW * 1000, false, false, false, null, null);
    }

    private static int DecodePeakCurrent(uint raw)
        => (int)((raw >> 20) & 0b11) switch
        {
            0b00 => 100,
            0b01 => 130,
            0b10 => 150,
            _ => 200
        };
}

public sealed record PdoDecodeResult
{
    [System.Text.Json.Serialization.JsonPropertyName("kind")] public string Kind { get; init; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("raw")] public uint Raw { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("minVoltageMv")] public int? MinVoltageMv { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("maxVoltageMv")] public int? MaxVoltageMv { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("maxCurrentMa")] public int? MaxCurrentMa { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("maxPowerMw")] public int? MaxPowerMw { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("dualRolePower")] public bool? DualRolePower { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("usbSuspendSupported")] public bool? UsbSuspendSupported { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("unconstrainedPower")] public bool? UnconstrainedPower { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("peakCurrent")] public int? PeakCurrent { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("notes")] public string? Notes { get; init; }

    public PdoDecodeResult() { }

    public PdoDecodeResult(
        string kind,
        uint raw,
        int? minVoltageMv,
        int? maxVoltageMv,
        int? maxCurrentMa,
        int? maxPowerMw,
        bool? dualRolePower,
        bool? usbSuspendSupported,
        bool? unconstrainedPower,
        int? peakCurrent,
        string? notes)
    {
        Kind = kind;
        Raw = raw;
        MinVoltageMv = minVoltageMv;
        MaxVoltageMv = maxVoltageMv;
        MaxCurrentMa = maxCurrentMa;
        MaxPowerMw = maxPowerMw;
        DualRolePower = dualRolePower;
        UsbSuspendSupported = usbSuspendSupported;
        UnconstrainedPower = unconstrainedPower;
        PeakCurrent = peakCurrent;
        Notes = notes;
    }
}
