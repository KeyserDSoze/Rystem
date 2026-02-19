using System.ComponentModel;

namespace Rystem.PlayFramework.Api.Models;

/// <summary>
/// Arguments model for the getUserConfirmation client tool.
/// JSON Schema is auto-generated from this class and sent to the LLM.
/// </summary>
public sealed class UserConfirmationArgs
{
    /// <summary>
    /// The question to ask the user for confirmation.
    /// </summary>
    [Description("The question to present to the user, e.g. 'Do you want to proceed with deleting your account?'")]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Label for the confirm button (default: "Yes").
    /// </summary>
    [Description("Text for the confirm/accept button")]
    public string ConfirmLabel { get; set; } = "Yes";

    /// <summary>
    /// Label for the cancel button (default: "No").
    /// </summary>
    [Description("Text for the cancel/reject button")]
    public string CancelLabel { get; set; } = "No";
}
