using System.ComponentModel;

namespace Rystem.PlayFramework.Api.Models;

/// <summary>
/// Arguments model for the trackAnalytics command.
/// </summary>
public sealed class TrackEventArgs
{
    /// <summary>
    /// The event name to track.
    /// </summary>
    [Description("The analytics event name (e.g., 'page_view', 'button_click')")]
    public string Event { get; set; } = string.Empty;
}
