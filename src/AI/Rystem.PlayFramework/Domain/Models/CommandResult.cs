namespace Rystem.PlayFramework.Domain.Models;

/// <summary>
/// Result of a client command execution.
/// Commands are fire-and-forget client tools that don't require immediate response.
/// </summary>
public sealed class CommandResult
{
    /// <summary>
    /// Indicates if the command executed successfully.
    /// </summary>
    public required bool Success { get; set; }

    /// <summary>
    /// Optional message to send to the LLM (e.g., error details, confirmation message).
    /// If null, only the success status is sent.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Creates a successful result without message.
    /// </summary>
    public static CommandResult Ok() => new() { Success = true };

    /// <summary>
    /// Creates a successful result with message.
    /// </summary>
    public static CommandResult Ok(string message) => new() { Success = true, Message = message };

    /// <summary>
    /// Creates a failed result with error message.
    /// </summary>
    public static CommandResult Fail(string message) => new() { Success = false, Message = message };

    /// <summary>
    /// Creates a failed result without message.
    /// </summary>
    public static CommandResult Fail() => new() { Success = false };
}
