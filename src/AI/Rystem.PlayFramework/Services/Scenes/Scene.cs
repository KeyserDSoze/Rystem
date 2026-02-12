using Microsoft.Extensions.AI;
using Rystem.PlayFramework.Mcp;

namespace Rystem.PlayFramework;

/// <summary>
/// Implementation of a scene.
/// </summary>
internal sealed class Scene : IScene
{
    private readonly SceneConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<ISceneTool> _tools;
    private readonly List<IActor> _actors;

    public Scene(SceneConfiguration config, IServiceProvider serviceProvider)
    {
        _config = config;
        _serviceProvider = serviceProvider;

        // Create tools
        _tools = _config.ServiceTools
            .Select(st => (ISceneTool)new ServiceMethodTool(st, serviceProvider))
            .ToList();

        // Create actors
        _actors = _config.Actors
            .Select(ac => ActorFactory.Create(ac, serviceProvider))
            .ToList();
    }

    public string Name => _config.Name;
    public string Description => _config.Description;
    public IReadOnlyList<McpServerReference> McpServerReferences => _config.McpServerReferences;

    public IEnumerable<ISceneTool> GetTools() => _tools;
    public IEnumerable<IActor> GetActors() => _actors;

    public async Task ExecuteActorsAsync(SceneContext context, CancellationToken cancellationToken = default)
    {
        foreach (var actor in _actors)
        {
            var response = await actor.PlayAsync(context, cancellationToken);
            
            if (!string.IsNullOrWhiteSpace(response.Message))
            {
                // Add system message - will be included in subsequent calls
                // Note: IChatClient is stateless, messages must be managed externally
                context.Properties[$"actor_message_{actor.GetType().Name}"] = response.Message;
            }
        }
    }
}
