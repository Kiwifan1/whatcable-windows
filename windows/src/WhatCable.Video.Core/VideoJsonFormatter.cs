using System.IO;
using System.Text.Json;

namespace WhatCable.Video.Core;

public static class VideoJsonFormatter
{
    private static readonly JsonSerializerOptions FormatterOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string Render<T>(T value)
        => JsonSerializer.Serialize(value, FormatterOptions);

    public static T? Parse<T>(string json)
        => JsonSerializer.Deserialize<T>(json, FormatterOptions);

    public static T? Parse<T>(Stream utf8Json)
        => JsonSerializer.Deserialize<T>(utf8Json, FormatterOptions);
}
