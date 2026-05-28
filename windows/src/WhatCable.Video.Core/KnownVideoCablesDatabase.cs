using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WhatCable.Video.Core;

/// <summary>
/// Database of known video cable certifications.
/// </summary>
public static class KnownVideoCablesDatabase
{
    private static readonly Lazy<IReadOnlyList<KnownVideoCable>> Cables = new(LoadCables);

    /// <summary>
    /// Get all known video cables.
    /// </summary>
    public static IReadOnlyList<KnownVideoCable> GetAll() => Cables.Value;

    /// <summary>
    /// Look up a cable by certification ID.
    /// </summary>
    public static KnownVideoCable? FindByCertificationId(string certificationId)
    {
        if (string.IsNullOrWhiteSpace(certificationId))
            return null;

        return Cables.Value.FirstOrDefault(c => c.CertificationId.Equals(certificationId, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Find cables by cable class.
    /// </summary>
    public static IReadOnlyList<KnownVideoCable> FindByClass(VideoCableClass cableClass)
    {
        return Cables.Value.Where(c => c.CableClass == cableClass).ToList();
    }

    /// <summary>
    /// Find cables by vendor.
    /// </summary>
    public static IReadOnlyList<KnownVideoCable> FindByVendor(string vendor)
    {
        if (string.IsNullOrWhiteSpace(vendor))
            return new List<KnownVideoCable>();

        return Cables.Value.Where(c => c.Vendor != null && c.Vendor.IndexOf(vendor, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList();
    }

    private static IReadOnlyList<KnownVideoCable> LoadCables()
    {
        using var stream = OpenResource("WhatCable.Video.Core.Resources.video-cables.json");
        return VideoJsonFormatter.Parse<List<KnownVideoCable>>(stream)
               ?? throw new InvalidOperationException("Unable to load embedded video cable catalog.");
    }

    private static Stream OpenResource(string resourceName)
        => Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
           ?? throw new InvalidOperationException($"Missing embedded resource '{resourceName}'.");
}
