using System.Text.Json.Serialization;

namespace Rystem.PlayFramework;

/// <summary>
/// Defines when a Command (fire-and-forget tool) should send feedback to the server.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommandFeedbackMode
{
    /// <summary>
    /// Never send feedback. Command is auto-completed immediately with success.
    /// Use for silent operations (logging, telemetry, UI updates) that don't require acknowledgment.
    /// </summary>
    Never,

    /// <summary>
    /// Send feedback only if command fails (default mode).
    /// On success: auto-complete immediately.
    /// On error: wait for client to send error details.
    /// </summary>
    OnError,

    /// <summary>
    /// Always send feedback (success + message).
    /// Server waits for client response even on success.
    /// Use for critical operations requiring confirmation (payments, data mutations).
    /// </summary>
    Always
}
