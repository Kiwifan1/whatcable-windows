using WhatCable.Video.Core;

namespace WhatCable.Windows.Backend.Video;

/// <summary>
/// Optional vendor GPU SDK adapter (NVAPI / AMD ADL/ADLX / Intel IGCL). Implementations
/// late-bind their native SDK via <c>LoadLibrary</c> and report <see cref="IsAvailable"/> =
/// <c>false</c> when the DLL is absent, so the default redistributable build carries no hard
/// dependency on any vendor SDK. When available, <see cref="QueryLink"/> returns the live
/// source-side link state for a display so it can be merged into the
/// <see cref="VideoPortSnapshot"/> as additive data.
/// </summary>
public interface IVendorGpuAdapter
{
    /// <summary>Human-readable vendor SDK name (e.g. <c>NVAPI</c>, <c>AMD ADL</c>, <c>Intel IGCL</c>).</summary>
    string Name { get; }

    /// <summary>True only when the vendor SDK loaded successfully and can be queried.</summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Returns the live link state for the supplied display, or <c>null</c> when the SDK
    /// cannot match or report on it (e.g. the display is driven by a different vendor's GPU).
    /// </summary>
    VendorGpuLinkInfo? QueryLink(DisplayInfo display);
}
