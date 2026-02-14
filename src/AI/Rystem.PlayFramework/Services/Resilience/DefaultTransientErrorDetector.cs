using System.Net;

namespace Rystem.PlayFramework;

/// <summary>
/// Default implementation of transient error detection for LLM calls.
/// Classifies exceptions as transient (retryable) or non-transient (fail-fast).
/// </summary>
internal sealed class DefaultTransientErrorDetector : ITransientErrorDetector
{
    /// <summary>
    /// Determines if an exception represents a transient error that can be retried.
    /// </summary>
    /// <remarks>
    /// Transient errors include:
    /// - Network timeouts (TaskCanceledException, OperationCanceledException)
    /// - HTTP 408 (Request Timeout)
    /// - HTTP 429 (Too Many Requests / Rate Limiting)
    /// - HTTP 5xx (Server Errors)
    /// - Model overload messages
    /// - Rate limit exceeded messages
    /// </remarks>
    public bool IsTransient(Exception exception)
    {
        return exception switch
        {
            TaskCanceledException => true,
            OperationCanceledException => true,
            HttpRequestException { StatusCode: HttpStatusCode.RequestTimeout } => true,
            HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } => true,
            HttpRequestException { StatusCode: >= HttpStatusCode.InternalServerError } => true,
            _ when exception.Message.Contains("model_overloaded", StringComparison.OrdinalIgnoreCase) => true,
            _ when exception.Message.Contains("rate_limit_exceeded", StringComparison.OrdinalIgnoreCase) => true,
            _ when exception.Message.Contains("overloaded", StringComparison.OrdinalIgnoreCase) => true,
            _ when exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }

    /// <summary>
    /// Determines if an exception represents a non-transient error that should fail immediately.
    /// </summary>
    /// <remarks>
    /// Non-transient errors include:
    /// - HTTP 401 (Unauthorized / Authentication Failure)
    /// - HTTP 403 (Forbidden / Permission Denied)
    /// - HTTP 400 (Bad Request / Invalid Input)
    /// - Content policy violations
    /// - Context length exceeded errors
    /// - Invalid API key messages
    /// </remarks>
    public bool IsNonTransient(Exception exception)
    {
        return exception switch
        {
            HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } => true,
            HttpRequestException { StatusCode: HttpStatusCode.Forbidden } => true,
            HttpRequestException { StatusCode: HttpStatusCode.BadRequest } => true,
            _ when exception.Message.Contains("content_policy_violation", StringComparison.OrdinalIgnoreCase) => true,
            _ when exception.Message.Contains("context_length_exceeded", StringComparison.OrdinalIgnoreCase) => true,
            _ when exception.Message.Contains("invalid_api_key", StringComparison.OrdinalIgnoreCase) => true,
            _ when exception.Message.Contains("invalid_request_error", StringComparison.OrdinalIgnoreCase) => true,
            _ when exception.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }
}
