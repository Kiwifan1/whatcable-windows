using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using WhatCable.Core;

namespace WhatCable.Windows.Backend;

/// <summary>
/// Renders a <see cref="WhatCableReport"/> to JSON. The base object is the macOS-schema
/// <see cref="Snapshot"/> (rendered through <see cref="JsonFormatter"/>); when not in
/// legacy mode the Windows-only <c>video</c> and <c>thunderbolt</c> sections are appended.
/// Output is key-sorted and 2-space indented to match the rest of the CLI payload.
/// </summary>
public static class ReportJsonRenderer
{
    private static readonly JsonSerializerOptions SectionOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Renders the report. When <paramref name="legacy"/> is true only the macOS-schema
    /// snapshot is emitted (the <c>video</c> and <c>thunderbolt</c> sections are suppressed).
    /// </summary>
    public static string Render(WhatCableReport report, bool legacy = false)
    {
        if (report is null)
        {
            throw new ArgumentNullException(nameof(report));
        }

        var root = JsonNode.Parse(JsonFormatter.Render(report.Snapshot))?.AsObject()
                   ?? throw new InvalidOperationException("Failed to serialize snapshot JSON.");

        if (!legacy)
        {
            root["video"] = JsonSerializer.SerializeToNode(report.Video, SectionOptions);
            root["thunderbolt"] = JsonSerializer.SerializeToNode(report.Thunderbolt, SectionOptions);
        }

        return SortJsonNode(root).ToJsonString(WriteOptions) + "\n";
    }

    private static JsonNode SortJsonNode(JsonNode node)
        => node switch
        {
            JsonObject obj => SortJsonObject(obj),
            JsonArray array => SortJsonArray(array),
            _ => node.DeepClone()
        };

    private static JsonObject SortJsonObject(JsonObject obj)
    {
        var sorted = new JsonObject();
        foreach (var property in obj.OrderBy(static property => property.Key, StringComparer.Ordinal))
        {
            sorted[property.Key] = property.Value is null ? null : SortJsonNode(property.Value);
        }

        return sorted;
    }

    private static JsonArray SortJsonArray(JsonArray array)
    {
        var sorted = new JsonArray();
        foreach (var item in array)
        {
            sorted.Add(item is null ? null : SortJsonNode(item));
        }

        return sorted;
    }
}
