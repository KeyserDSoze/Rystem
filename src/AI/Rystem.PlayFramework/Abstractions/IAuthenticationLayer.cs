using Microsoft.AspNetCore.Http;

namespace Rystem.PlayFramework;

/// <summary>
/// Resolves the current user's identifier from an HTTP request.
/// Register an implementation via <see cref="PlayFrameworkBuilder.AddAuthenticationLayer{T}"/>.
/// When registered, this is the highest-priority source for userId in conversation endpoints —
/// it runs before claims, <see cref="HttpContext"/> items, and <see cref="IAuthorizationLayer"/>.
/// </summary>
public interface IAuthenticationLayer
{
    /// <summary>
    /// Returns an <see cref="AuthenticationResult"/> for the current request.
    /// Return a result with a null <see cref="AuthenticationResult.UserId"/> if this layer
    /// cannot resolve one (the next fallback will be tried).
    /// </summary>
    Task<AuthenticationResult?> ExecuteAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
