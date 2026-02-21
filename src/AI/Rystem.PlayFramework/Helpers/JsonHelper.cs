using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Rystem.PlayFramework.Helpers;

internal static partial class JsonHelper
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };
}
