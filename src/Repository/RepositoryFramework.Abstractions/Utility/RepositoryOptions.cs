using System.Text.Json;

namespace RepositoryFramework
{
    public static class RepositoryOptions
    {
        public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
