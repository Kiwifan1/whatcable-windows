using System.Text.Json;
using System.Text.Json.Serialization;
using WhatCable.Core;

namespace WhatCable.Widgets.Core;

/// <summary>
/// A single video-link diagnostic verdict surfaced on the large widget. Kept deliberately small and
/// independent of <c>WhatCable.Video.Core</c> so the widget core stays a lean, portable library;
/// the app maps its richer <c>VideoPortReport</c> values onto this shape before publishing.
/// </summary>
public sealed record VideoVerdict
{
    [JsonPropertyName("displayName")] public string DisplayName { get; init; } = string.Empty;
    [JsonPropertyName("verdict")] public string Verdict { get; init; } = string.Empty;

    public VideoVerdict() { }

    public VideoVerdict(string displayName, string verdict)
        => (DisplayName, Verdict) = (displayName, verdict);
}

/// <summary>
/// The payload the WinUI app pushes to the widget provider over the named pipe. Wraps the
/// macOS-schema <see cref="Snapshot"/> with the optional per-display video verdicts the large
/// widget renders.
/// </summary>
public sealed record WidgetPayload
{
    [JsonPropertyName("snapshot")] public Snapshot Snapshot { get; init; } = new();
    [JsonPropertyName("videoVerdicts")] public IReadOnlyList<VideoVerdict> VideoVerdicts { get; init; } = Array.Empty<VideoVerdict>();

    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Serializes the payload to a single-line JSON string for transport over the pipe.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, Options);

    /// <summary>Parses a payload from its JSON representation. Throws on malformed input.</summary>
    public static WidgetPayload FromJson(string json)
        => JsonSerializer.Deserialize<WidgetPayload>(json, Options)
           ?? throw new InvalidOperationException("Failed to parse widget payload JSON.");
}
