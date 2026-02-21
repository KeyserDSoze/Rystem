namespace Rystem.PlayFramework;

public interface IContext
{
    Task<dynamic?> RetrieveAsync(SceneContext context, SceneRequestSettings settings, CancellationToken cancellationToken);
}
