namespace Rystem.PlayFramework;

/// <summary>
/// Web search freshness filter (time range for results).
/// </summary>
public enum WebSearchFreshness
{
    /// <summary>
    /// No time filtering (all results).
    /// </summary>
    Any = 0,
    
    /// <summary>
    /// Results from the last 24 hours.
    /// </summary>
    Day = 1,
    
    /// <summary>
    /// Results from the last 7 days.
    /// </summary>
    Week = 2,
    
    /// <summary>
    /// Results from the last 30 days.
    /// </summary>
    Month = 3,
    
    /// <summary>
    /// Results from the last 365 days.
    /// </summary>
    Year = 4
}
