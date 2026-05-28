using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace WhatCable.Video.Core;

public static class VideoJsonFormatter
{
    private static readonly JsonSerializerOptions FormatterOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Render<T>(T value)
    {
        var node = JsonSerializer.SerializeToNode(value, FormatterOptions)
                   ?? throw new InvalidOperationException("Failed to serialize video JSON.");
        return SortJsonNode(node).ToJsonString(FormatterOptions) + "\n";
    }

    public static T? Parse<T>(string json)
        => JsonSerializer.Deserialize<T>(json, FormatterOptions);

    public static T? Parse<T>(Stream utf8Json)
        => JsonSerializer.Deserialize<T>(utf8Json, FormatterOptions);

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
