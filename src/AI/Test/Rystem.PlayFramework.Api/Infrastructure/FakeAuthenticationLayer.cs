using Microsoft.AspNetCore.Http;

namespace Rystem.PlayFramework.Api.Infrastructure;

/// <summary>
/// Fake <see cref="IAuthenticationLayer"/> for the test project.
/// Always returns a fixed userId so that conversation endpoints can
/// persist and load conversations without requiring real authentication.
/// Replace with a real JWT/claims-based implementation in production.
/// </summary>
public sealed class FakeAuthenticationLayer : IAuthenticationLayer
{
    private const string FakeUserId = "alessandro.rapiti44@gmail.com";

    public Task<AuthenticationResult?> ExecuteAsync(HttpContext httpContext, CancellationToken cancellationToken)
        => Task.FromResult<AuthenticationResult?>(new AuthenticationResult { UserId = FakeUserId });
}
