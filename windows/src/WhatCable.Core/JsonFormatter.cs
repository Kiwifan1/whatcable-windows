using System.Text.Json;
using System.Text.Json.Serialization;
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
        => JsonSerializer.Serialize(snapshot, JsonContext.Snapshot) + "\n";

    public static Snapshot Parse(string json)
        => JsonSerializer.Deserialize(json, JsonContext.Snapshot)
           ?? throw new InvalidOperationException("Failed to parse snapshot JSON.");
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
