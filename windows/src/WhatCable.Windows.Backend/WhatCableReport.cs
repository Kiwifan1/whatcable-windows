using WhatCable.Core;
using WhatCable.Video.Core;
using WhatCable.Windows.Backend.Thunderbolt;

namespace WhatCable.Windows.Backend;

/// <summary>
/// The full Windows snapshot: the macOS-schema <see cref="Snapshot"/> plus the new Windows
/// sections (the per-display <see cref="VideoPortReport"/> list and the Thunderbolt chain).
/// The new sections are suppressed by the CLI's <c>--legacy</c> flag for tools that still
/// consume the macOS schema.
/// </summary>
public sealed record WhatCableReport
{
    public Snapshot Snapshot { get; init; } = new();
    public IReadOnlyList<VideoPortReport> Video { get; init; } = Array.Empty<VideoPortReport>();
    public IReadOnlyList<ThunderboltDevice> Thunderbolt { get; init; } = Array.Empty<ThunderboltDevice>();
}
