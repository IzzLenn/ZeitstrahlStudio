using System.Text.Json;

namespace ZeitstrahlStudio.Export;

/// <summary>Serialisiert Exportdaten mit dem sicheren Standard-Encoder für einen JSON-Scriptblock.</summary>
public static class StandaloneHtmlDataEncoder
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(value, Options);
    }
}
