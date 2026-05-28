using System.Reflection;
using System.Text.Json;

namespace WhatCable.Core;

public static class CableDatabase
{
    private static readonly Lazy<Dictionary<CableFingerprint, CuratedCable>> Cables = new(LoadCables);

    public static CuratedCable? CuratedCable(int vid, int pid, uint cableVdo)
    {
        if (vid == 0 && pid == 0 && cableVdo == 0) return null;
        return Cables.Value.TryGetValue(new CableFingerprint(vid, pid, cableVdo), out var value) ? value : null;
    }

    private static Dictionary<CableFingerprint, CuratedCable> LoadCables()
    {
        using var stream = OpenResource("WhatCable.Core.Resources.cables.json");
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.EnumerateArray().ToDictionary(
            x => new CableFingerprint(x.GetProperty("vid").GetInt32(), x.GetProperty("pid").GetInt32(), x.GetProperty("cableVDO").GetUInt32()),
            x => new CuratedCable(x.GetProperty("brand").GetString()!, x.GetProperty("speed").GetString()!, x.GetProperty("power").GetString()!, x.GetProperty("type").GetString()!, x.GetProperty("issueURL").GetString()!));
    }
}

public static class VendorDatabase
{
    private static readonly Lazy<Dictionary<int, VendorEntry>> Vendors = new(LoadVendors);
    public static bool HasAuthoritativeUsbIfRegistry => false;

    public static string? Name(int vendorId)
    {
        if (vendorId == 0xFFFF) return "No vendor ID assigned (USB-PD spec sentinel)";
        return Vendors.Value.TryGetValue(vendorId, out var v) ? v.Name : null;
    }

    public static bool IsRegistered(int vendorId)
    {
        if (vendorId == 0xFFFF) return false;
        return Vendors.Value.TryGetValue(vendorId, out var v) && string.Equals(v.Source, "usbif", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<int, VendorEntry> LoadVendors()
    {
        using var stream = OpenResource("WhatCable.Core.Resources.vendors.json");
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.EnumerateArray().ToDictionary(
            x => x.GetProperty("vid").GetInt32(),
            x => new VendorEntry(x.GetProperty("name").GetString()!, x.GetProperty("source").GetString()!));
    }

    private static Stream OpenResource(string resourceName)
        => Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
           ?? throw new InvalidOperationException($"Missing embedded resource '{resourceName}'.");
}

public sealed record CuratedCable(string Brand, string Speed, string Power, string Type, string IssueUrl);
public sealed record CableFingerprint(int Vid, int Pid, uint CableVdo);
public sealed record VendorEntry(string Name, string Source);
