namespace Rystem.PlayFramework;

public interface IContext
{
    Task<object?> RetrieveAsync(SceneContext context, SceneRequestSettings settings, CancellationToken cancellationToken);
}
