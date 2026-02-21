using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Services.Helpers;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Shared dependencies for all execution mode handlers.
/// </summary>
internal sealed class ExecutionModeHandlerDependencies : IFactoryName
{
    private readonly IServiceProvider _serviceProvider;

    public ISceneFactory SceneFactory { get; private set; } = null!;
    public IResponseHelper ResponseHelper { get; private set; } = null!;
    public IStreamingHelper StreamingHelper { get; private set; } = null!;
    public IPlayFrameworkCache PlayFrameworkCache { get; private set; } = null!;
    public PlayFrameworkSettings Settings { get; private set; } = null!;
    public ILogger Logger { get; private set; } = null!;
    public string FactoryName { get; private set; } = "default";

    public ExecutionModeHandlerDependencies(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public bool FactoryNameAlreadySetup { get; set; }
    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        FactoryName = name?.ToString() ?? "default";

        var settingsFactory = _serviceProvider.GetRequiredService<IFactory<PlayFrameworkSettings>>();
        var sceneFactoryFactory = _serviceProvider.GetRequiredService<IFactory<ISceneFactory>>();
        var playFrameworkCacheFactory = _serviceProvider.GetRequiredService<IFactory<IPlayFrameworkCache>>();
        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

        SceneFactory = sceneFactoryFactory.Create(name) 
            ?? throw new InvalidOperationException($"SceneFactory not found for factory: {name}");
        ResponseHelper = _serviceProvider.GetRequiredService<IResponseHelper>();
        StreamingHelper = _serviceProvider.GetRequiredService<IStreamingHelper>();
        PlayFrameworkCache = playFrameworkCacheFactory.Create(name)
            ?? throw new InvalidOperationException($"PlayFrameworkCache not found for factory: {name}");
        Settings = settingsFactory.Create(name) ?? new PlayFrameworkSettings();
        Logger = loggerFactory.CreateLogger<SceneManager>();
    }
}
