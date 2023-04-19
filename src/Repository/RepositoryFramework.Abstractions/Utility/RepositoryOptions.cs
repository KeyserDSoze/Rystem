using System.Text.Json;

namespace RepositoryFramework
{
    public static class RepositoryOptions
    {
        public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
        {
            DefaultBufferSize = 128,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
