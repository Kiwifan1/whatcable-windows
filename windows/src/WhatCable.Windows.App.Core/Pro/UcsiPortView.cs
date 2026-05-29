using System.Text.Json;
using WhatCable.Core;

namespace WhatCable.Windows.App.Core.Pro;

/// <summary>
/// A read-only projection of the fields the Pro features need from a port's serialized UCSI
/// payload (<see cref="Port.Ucsi"/>). The backend serializes the full <c>UcsiConnector</c> into
/// that JSON element, so the app can consume UCSI detail without taking a (Windows-only) reference
/// on <c>WhatCable.Windows.Backend</c>.
/// </summary>
public sealed record UcsiPortView
{
    /// <summary>Well-known Standard / Vendor IDs used to classify alternate modes.</summary>
    public const int DisplayPortSvid = 0xFF01;
    public const int ThunderboltSvid = 0x8087;

    /// <summary>UCSI BCD version (e.g. 0x0200 for 2.0), or 0 when unknown.</summary>
    public int VersionBcd { get; init; }

    /// <summary>True when the connector reports UCSI 2.0 or newer (liquid detection / PD message).</summary>
    public bool IsUcsi20 => VersionBcd >= 0x0200;

    /// <summary>Alternate modes from <c>GET_ALTERNATE_MODES</c>.</summary>
    public IReadOnlyList<UcsiAltModeView> AlternateModes { get; init; } = Array.Empty<UcsiAltModeView>();

    /// <summary>Decoded cable e-marker speed (USB 2.0 / 3.2 / USB4), or null when no e-marker.</summary>
    public CableSpeed? CableSpeed { get; init; }

    /// <summary>True when the connector advertises USB 3.x SuperSpeed capability.</summary>
    public bool Usb3Capable { get; init; }

    /// <summary>True when the partner is attached (a device/cable is connected).</summary>
    public bool Connected { get; init; }

    /// <summary>UCSI 2.0 liquid-detection state, or null when the platform doesn't implement it.</summary>
    public bool? LiquidDetected { get; init; }

    /// <summary>True when the connector exposes the liquid-detection field at all.</summary>
    public bool LiquidDetectionSupported { get; init; }

    /// <summary>The currently entered alternate mode (per GET_CURRENT_CAM), or null when none.</summary>
    public UcsiAltModeView? ActiveMode => AlternateModes.FirstOrDefault(m => m.Active);

    /// <summary>True when a DisplayPort alternate mode is currently entered.</summary>
    public bool DisplayPortActive
        => AlternateModes.Any(m => m.Active && m.Svid == DisplayPortSvid);

    /// <summary>True when a Thunderbolt alternate mode is currently entered.</summary>
    public bool ThunderboltActive
        => AlternateModes.Any(m => m.Active && m.Svid == ThunderboltSvid);

    /// <summary>
    /// Parses the serialized UCSI payload on a <see cref="Port"/>. Returns null when the port has
    /// no UCSI data (i.e. a non-UCSI host or a plain USB host-controller port).
    /// </summary>
    public static UcsiPortView? FromPort(Port port)
    {
        ArgumentNullException.ThrowIfNull(port);
        return port.Ucsi is { } element ? FromElement(element) : null;
    }

    /// <summary>Parses a serialized <c>UcsiConnector</c> JSON element.</summary>
    public static UcsiPortView FromElement(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return new UcsiPortView();
        }

        var modes = new List<UcsiAltModeView>();
        if (element.TryGetProperty("alternateModes", out var modesEl) && modesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var mode in modesEl.EnumerateArray())
            {
                modes.Add(new UcsiAltModeView
                {
                    Svid = GetInt(mode, "svid"),
                    Vdo = (uint)GetLong(mode, "vdo"),
                    Name = GetString(mode, "name"),
                    Active = GetBool(mode, "active") ?? false,
                });
            }
        }

        CableSpeed? cableSpeed = null;
        if (element.TryGetProperty("cableVdo", out var vdoEl) && vdoEl.ValueKind == JsonValueKind.Object
            && vdoEl.TryGetProperty("Speed", out var speedEl))
        {
            cableSpeed = speedEl.ValueKind switch
            {
                JsonValueKind.Number when speedEl.TryGetInt32(out var n) => (CableSpeed)n,
                JsonValueKind.String when Enum.TryParse<CableSpeed>(speedEl.GetString(), out var s) => s,
                _ => null,
            };
        }

        var usb3 = false;
        if (element.TryGetProperty("capability", out var capEl) && capEl.ValueKind == JsonValueKind.Object)
        {
            usb3 = GetBool(capEl, "usb3") ?? false;
        }

        var connected = false;
        if (element.TryGetProperty("status", out var statusEl) && statusEl.ValueKind == JsonValueKind.Object)
        {
            connected = GetBool(statusEl, "connected") ?? false;
        }

        var liquidSupported = false;
        if (element.TryGetProperty("available", out var availEl) && availEl.ValueKind == JsonValueKind.Object)
        {
            liquidSupported = GetBool(availEl, "liquidDetection") ?? false;
        }

        return new UcsiPortView
        {
            VersionBcd = GetInt(element, "versionBcd"),
            AlternateModes = modes,
            CableSpeed = cableSpeed,
            Usb3Capable = usb3,
            Connected = connected,
            LiquidDetected = GetBool(element, "liquidDetected"),
            LiquidDetectionSupported = liquidSupported,
        };
    }

    private static int GetInt(JsonElement obj, string name)
        => obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v) ? v : 0;

    private static long GetLong(JsonElement obj, string name)
        => obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var v) ? v : 0;

    private static string? GetString(JsonElement obj, string name)
        => obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static bool? GetBool(JsonElement obj, string name)
        => obj.TryGetProperty(name, out var el) && el.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? el.GetBoolean()
            : null;
}

/// <summary>A single alternate mode projected from the serialized UCSI payload.</summary>
public sealed record UcsiAltModeView
{
    public int Svid { get; init; }
    public uint Vdo { get; init; }
    public string? Name { get; init; }
    public bool Active { get; init; }
}
