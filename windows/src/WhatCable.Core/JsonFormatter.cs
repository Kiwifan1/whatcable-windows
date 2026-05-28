using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;

namespace WhatCable.Core;

public static class JsonFormatter
{
    private static readonly WhatCableJsonContext JsonContext = new(new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });

    public static string Render(Snapshot snapshot)
    {
        var node = JsonSerializer.SerializeToNode(snapshot, JsonContext.Snapshot)
                   ?? throw new InvalidOperationException("Failed to serialize snapshot JSON.");
        return SortJsonNode(node).ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }) + "\n";
    }

    public static Snapshot Parse(string json)
        => JsonSerializer.Deserialize(json, JsonContext.Snapshot)
           ?? throw new InvalidOperationException("Failed to parse snapshot JSON.");

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

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Snapshot))]
[JsonSerializable(typeof(Port))]
[JsonSerializable(typeof(Cable))]
[JsonSerializable(typeof(TrustFlag))]
[JsonSerializable(typeof(AdapterSnapshot))]
[JsonSerializable(typeof(ChargerProfile))]
[JsonSerializable(typeof(PdoDecodeResult))]
[JsonSerializable(typeof(DeviceNode))]
internal partial class WhatCableJsonContext : JsonSerializerContext;
