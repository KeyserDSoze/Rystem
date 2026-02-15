using System.Text.Json.Serialization;

namespace Rystem.PlayFramework;

/// <summary>
/// Defines cache behavior for scene executions.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CacheBehavior
{
    /// <summary>
    /// Default behavior: read from cache on request, write after execution.
    /// </summary>
    Default,

    /// <summary>
    /// Skip cache completely (useful for real-time data).
    /// </summary>
    Avoidable,

    /// <summary>
    /// Cache forever (useful for static content).
    /// </summary>
    Forever
}
