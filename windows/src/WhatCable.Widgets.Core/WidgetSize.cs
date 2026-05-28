namespace WhatCable.Widgets.Core;

/// <summary>
/// The three widget sizes the WhatCable provider renders. Mirrors the
/// <c>$host.widgetSize</c> values (<c>small</c>, <c>medium</c>, <c>large</c>) reported by the
/// Windows 11 Widgets Board host.
/// </summary>
public enum WidgetSize
{
    Small,
    Medium,
    Large,
}

/// <summary>Helpers for mapping between the host's size strings and <see cref="WidgetSize"/>.</summary>
public static class WidgetSizes
{
    /// <summary>
    /// Maps a host size name (case-insensitive <c>small</c> / <c>medium</c> / <c>large</c>) to a
    /// <see cref="WidgetSize"/>. Unknown or empty values fall back to <see cref="WidgetSize.Medium"/>,
    /// the host's default pinned size.
    /// </summary>
    public static WidgetSize FromHostName(string? hostSizeName) => hostSizeName?.Trim().ToLowerInvariant() switch
    {
        "small" => WidgetSize.Small,
        "large" => WidgetSize.Large,
        _ => WidgetSize.Medium,
    };

    /// <summary>The host size string for a <see cref="WidgetSize"/>.</summary>
    public static string ToHostName(WidgetSize size) => size switch
    {
        WidgetSize.Small => "small",
        WidgetSize.Large => "large",
        _ => "medium",
    };
}
