using WhatCable.Video.Core;

namespace WhatCable.Windows.Backend.Video;

/// <summary>
/// A display path discovered through <c>QueryDisplayConfig</c>, captured as a
/// platform-neutral record so the mapping to <see cref="VideoPortSnapshot"/> can be
/// unit-tested against recorded <c>QueryDisplayConfig</c> + EDID pairs.
/// </summary>
public sealed record DisplayInfo
{
    /// <summary>Monitor friendly name (from <c>DisplayConfigGetDeviceInfo</c>), if available.</summary>
    public string? Name { get; init; }

    /// <summary>Physical connector reported by <c>QueryDisplayConfig</c>.</summary>
    public VideoConnectorType ConnectorType { get; init; }

    /// <summary>Currently active timing (resolution + refresh) for the path, if the path is active.</summary>
    public VideoMode? ActiveMode { get; init; }

    /// <summary>True when HDR / advanced color is currently enabled on the path.</summary>
    public bool HdrEnabled { get; init; }

    /// <summary>Current color encoding (e.g. <c>RGB</c>, <c>YCbCr444</c>), if known.</summary>
    public string? ColorEncoding { get; init; }

    /// <summary>Raw EDID bytes read from the registry / monitor IOCTL, if available.</summary>
    public byte[]? EdidRaw { get; init; }
}
