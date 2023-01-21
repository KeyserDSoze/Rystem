using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    public sealed record FilterOperationAsString(
        [property: JsonPropertyName("q")] FilterOperations Operation,
        [property: JsonPropertyName("v")] string? Value = null);
}
