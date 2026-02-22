namespace Rystem.PlayFramework;

public interface IAuthorizationLayer
{
    Task<AuthorizationResult> AuthorizeAsync(SceneContext context, SceneRequestSettings settings, CancellationToken cancellationToken);
}
