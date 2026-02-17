using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Services.Helpers;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Shared dependencies for all execution mode handlers.
/// </summary>
internal sealed class ExecutionModeHandlerDependencies : IFactoryName
{
    public required ISceneFactory SceneFactory { get; init; }
    public required IResponseHelper ResponseHelper { get; init; }
    public required IStreamingHelper StreamingHelper { get; init; }
    public required ISceneMatchingHelper SceneMatchingHelper { get; init; }
    public required PlayFrameworkSettings Settings { get; init; }
    public required ILogger Logger { get; init; }
    public required string FactoryName { get; init; }

    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        throw new NotImplementedException();
    }
}
