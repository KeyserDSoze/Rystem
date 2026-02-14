namespace Rystem.PlayFramework;

/// <summary>
/// Service for detecting transient vs non-transient errors in LLM calls.
/// Transient errors can be retried, non-transient errors should fail immediately.
/// </summary>
public interface ITransientErrorDetector
{
    /// <summary>
    /// Determines if an exception represents a transient error that can be retried.
    /// Examples: network timeouts, rate limits, server errors (5xx).
    /// </summary>
    bool IsTransient(Exception exception);

    /// <summary>
    /// Determines if an exception represents a non-transient error that should fail immediately.
    /// Examples: authentication failures (401, 403), bad requests (400), content policy violations.
    /// </summary>
    bool IsNonTransient(Exception exception);
}
