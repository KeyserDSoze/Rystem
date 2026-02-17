using System.Runtime.CompilerServices;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Handles Scene execution mode - directly executes a specific scene by name.
/// </summary>
internal sealed class SceneExecutionHandler : IExecutionModeHandler
{
    private readonly ExecutionModeHandlerDependencies _dependencies;
    private readonly ISceneExecutor _sceneExecutor;

    public SceneExecutionHandler(
        ExecutionModeHandlerDependencies dependencies,
        ISceneExecutor sceneExecutor)
    {
        _dependencies = dependencies;
        _sceneExecutor = sceneExecutor;
    }

    public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        SceneContext context,
        SceneRequestSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Direct scene execution by name (bypasses scene selection)
        if (string.IsNullOrEmpty(settings.SceneName))
        {
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.Error,
                ErrorMessage = "Scene mode requested but SceneName is not set in SceneRequestSettings"
            });

            yield break;
        }

        var targetScene = _dependencies.SceneFactory.Create(settings.SceneName);
        if (targetScene == null)
        {
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.Error,
                ErrorMessage = $"Scene '{settings.SceneName}' not found"
            });

            yield break;
        }

        await foreach (var response in _sceneExecutor.ExecuteSceneAsync(context, targetScene, settings, cancellationToken))
        {
            yield return response;
        }
    }

    private static AiSceneResponse YieldAndTrack(SceneContext context, AiSceneResponse response)
    {
        response.TotalCost = context.TotalCost;
        context.Responses.Add(response);
        return response;
    }
}
