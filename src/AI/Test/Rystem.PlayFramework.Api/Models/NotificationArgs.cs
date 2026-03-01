using System.ComponentModel;

namespace Rystem.PlayFramework.Api.Models;

/// <summary>
/// Arguments model for the showNotification command.
/// </summary>
public sealed class NotificationArgs
{
    /// <summary>
    /// The notification title.
    /// </summary>
    [Description("The notification title to display")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The notification body message.
    /// </summary>
    [Description("The notification body text")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The notification type (info, success, warning, error).
    /// </summary>
    [Description("The notification type: 'info', 'success', 'warning', or 'error'")]
    public string Type { get; set; } = "info";
}
