namespace WhatCable.Windows.Backend.Video;

/// <summary>
/// Discovers the optional vendor GPU adapters (NVAPI / AMD ADL / Intel IGCL). Each adapter
/// late-binds its native SDK in its constructor and reports
/// <see cref="IVendorGpuAdapter.IsAvailable"/>; only those whose SDK actually loaded are
/// returned, so on a stock machine with no vendor SDK present the result is empty.
/// </summary>
/// <remarks>
/// Discovery is gated by the <c>EnableVendorGpuAdapters</c> feature flag: the default
/// redistributable / CI build leaves it off, while installer builds turn it on. Callers pass
/// the resolved flag to <see cref="Discover"/>.
/// </remarks>
public static class VendorGpuAdapters
{
    /// <summary>
    /// Returns the vendor GPU adapters whose SDK loaded successfully when
    /// <paramref name="enabled"/> is <c>true</c>; otherwise returns an empty list.
    /// </summary>
    public static IReadOnlyList<IVendorGpuAdapter> Discover(bool enabled)
    {
        if (!enabled)
        {
            return Array.Empty<IVendorGpuAdapter>();
        }

        var candidates = new IVendorGpuAdapter[]
        {
            new NvApiVendorGpuAdapter(),
            new AmdAdlVendorGpuAdapter(),
            new IntelIgclVendorGpuAdapter(),
        };

        var available = new List<IVendorGpuAdapter>(candidates.Length);
        foreach (var candidate in candidates)
        {
            if (candidate.IsAvailable)
            {
                available.Add(candidate);
            }
            else if (candidate is IDisposable disposable)
            {
                // Release any SDK handle a candidate opened before deciding it was unusable.
                disposable.Dispose();
            }
        }

        return available;
    }
}
