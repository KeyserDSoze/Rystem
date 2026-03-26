namespace Rystem.PlayFramework.Api.Infrastructure;

/// <summary>
/// Fake IAuthorizationLayer for the test project.
/// Always grants access and injects a fixed userId so that SceneManager
/// can persist/load conversations keyed by that user.
/// In production replace this with a real JWT/claims-based implementation.
/// </summary>
public sealed class FakeAuthorizationLayer : IAuthorizationLayer
{
    private const string FakeUserId = "alessandro.rapiti44@gmail.com";

    public Task<AuthorizationResult> AuthorizeAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new AuthorizationResult
        {
            IsAuthorized = true,
            UserId = FakeUserId
        });
    }
}
