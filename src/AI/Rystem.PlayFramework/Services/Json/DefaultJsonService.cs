using System.Text.Json;

namespace Rystem.PlayFramework;

/// <summary>
/// Default JSON service implementation using System.Text.Json.
/// </summary>
internal sealed class DefaultJsonService : IJsonService
{
    private readonly JsonSerializerOptions _options;

    public DefaultJsonService()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public DefaultJsonService(JsonSerializerOptions options)
    {
        _options = options;
    }

    public string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, _options);

    public string Serialize(object? value, Type type)
        => JsonSerializer.Serialize(value, type, _options);

    public T? Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, _options);

    public object? Deserialize(string json, Type type)
        => JsonSerializer.Deserialize(json, type, _options);
}
