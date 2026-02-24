using System.ComponentModel;

namespace Rystem.PlayFramework.Api.Models;

/// <summary>
/// Arguments model for the logUserAction command.
/// </summary>
public sealed class LogActionArgs
{
    /// <summary>
    /// The action to log in the browser console.
    /// </summary>
    [Description("The user action to log (e.g., 'button_clicked', 'form_submitted')")]
    public string Action { get; set; } = string.Empty;
}
