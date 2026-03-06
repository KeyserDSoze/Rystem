using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Handles Scene execution mode - directly executes a specific scene by name.
/// </summary>
internal sealed class SceneExecutionHandler : IExecutionModeHandler
{
    private readonly IFactory<ExecutionModeHandlerDependencies> _dependenciesFactory;
    private readonly IFactory<ISceneExecutor> _sceneExecutorFactory;
    private readonly ILogger<SceneExecutionHandler> _logger;

    public SceneExecutionHandler(
        IFactory<ExecutionModeHandlerDependencies> dependenciesFactory,
        IFactory<ISceneExecutor> sceneExecutorFactory,
        ILogger<SceneExecutionHandler> logger)
    {
        _dependenciesFactory = dependenciesFactory;
        _sceneExecutorFactory = sceneExecutorFactory;
        _logger = logger;
    }

    public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        AnyOf<string?, Enum>? factoryName,
        SceneContext context,
        SceneRequestSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Resolve dependencies from factory
        var dependencies = _dependenciesFactory.Create(factoryName)
            ?? throw new InvalidOperationException($"ExecutionModeHandlerDependencies not found for factory: {factoryName}");

        var sceneExecutor = _sceneExecutorFactory.Create(factoryName)
            ?? throw new InvalidOperationException($"SceneExecutor not found for factory: {factoryName}");

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

        _logger.LogInformation("Scene mode: routing directly to '{SceneName}' (Factory: {FactoryName})",
            settings.SceneName, factoryName?.ToString() ?? "default");

        var targetScene = dependencies.SceneFactory.TryGetScene(settings.SceneName);
        if (targetScene == null)
        {
            _logger.LogWarning("Scene '{SceneName}' not found in Scene execution mode (Factory: {FactoryName})",
                settings.SceneName, factoryName?.ToString() ?? "default");

            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.Error,
                ErrorMessage = $"Scene '{settings.SceneName}' not found"
            });

            yield break;
        }

        await foreach (var response in sceneExecutor.ExecuteSceneAsync(context, targetScene, settings, cancellationToken))
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
