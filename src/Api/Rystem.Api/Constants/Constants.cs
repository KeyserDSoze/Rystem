using System.Text.Json;

namespace Rystem.Api
{
    public static class Constants
    {
        public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
        {
            PropertyNameCaseInsensitive = true,
        };
    }
}
