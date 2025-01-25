using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rystem.Api
{
    public static class DefaultJsonSettings
    {
        public static readonly JsonSerializerOptions ForEnum = new()
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
    }
}
