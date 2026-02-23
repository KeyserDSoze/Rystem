namespace Rystem.PlayFramework.Api.Models;

/// <summary>
/// Request body for updating conversation visibility.
/// </summary>
public sealed class UpdateConversationVisibilityRequest
{
    /// <summary>
    /// Whether the conversation should be public (accessible to everyone).
    /// </summary>
    public required bool IsPublic { get; set; }
}
