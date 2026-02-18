using Microsoft.Extensions.AI;
using Rystem.PlayFramework.Configuration;
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

    public string Description
    {
        get
        {
            if (!_config.AutoGenerateToolDescription)
            {
                return _config.Description; // Standard behavior - return configured description
            }

            var toolNames = _tools.Select(t => t.Name).ToList();

            if (toolNames.Count == 0)
            {
                return _config.Description; // No tools available, return original description
            }

            var toolList = $"Available tools: {string.Join(", ", toolNames)}";

            if (string.IsNullOrWhiteSpace(_config.Description))
            {
                // No manual description - generate a default one
                return $"This scene provides the following capabilities: {string.Join(", ", toolNames)}";
            }

            // Manual description exists - append tool list
            var description = _config.Description.TrimEnd();
            var separator = description.EndsWith('.') ? " " : ". ";
            return $"{description}{separator}{toolList}";
        }
    }

    public IReadOnlyList<McpServerReference> McpServerReferences => _config.McpServerReferences;
    public IReadOnlyList<ClientInteractionDefinition>? ClientInteractionDefinitions => _config.ClientInteractionDefinitions;
    public TimeSpan CacheExpiration => _config.CacheExpiration;

    public IEnumerable<ISceneTool> GetTools() => _tools;
    public IEnumerable<IActor> GetActors() => _actors;

    public async Task ExecuteActorsAsync(SceneContext context, CancellationToken cancellationToken = default)
    {
        foreach (var actor in _actors)
        {
            var response = await actor.PlayAsync(context, cancellationToken);

            if (!string.IsNullOrWhiteSpace(response.Message))
            {
                // Add scene actor message to conversation history
                // This will be included in LLM requests and cached
                context.AddSceneActorMessage(Name, actor.GetType().Name, response.Message);
            }
        }
    }
}
