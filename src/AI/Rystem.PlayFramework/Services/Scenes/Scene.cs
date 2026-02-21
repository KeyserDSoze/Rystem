using System.Text.Json;
using Microsoft.Extensions.AI;
using Rystem.PlayFramework.Configuration;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework;

/// <summary>
/// Implementation of a scene.
/// </summary>
internal sealed class Scene : IScene
{
    private readonly SceneConfiguration _config;
    public List<IActor> Actors { get; }
    public List<AITool> AiTools { get; }
    public List<ISceneTool> Tools { get; }
    public AITool AiTool { get; }
    public Scene(SceneConfiguration configuration)
    {
        _config = configuration;

        // Create tools from service methods
        var serviceTools = _config.ServiceTools
            .Select(st => new ServiceMethodTool(st))
            .ToList();

        // Create tools from client interactions (OnClient)
        var clientTools = _config.ClientInteractionDefinitions
            ?.Select(def => new ClientInteractionTool(def))
            .ToList() ?? [];

        // Combine all tools
        Tools = [.. serviceTools];
        Tools.AddRange(clientTools);
        AiTools = [.. Tools.Select(x => x.ToolDescription)];
        // Create actors
        Actors = [.. _config.Actors.Select(ac => ActorFactory.Create(ac))];


        Description = CreateDescription(configuration, Tools);
        AiTool = AIFunctionFactory.Create(
            () => configuration.Name,
            configuration.Name,
            configuration.Description,
            JsonHelper.JsonSerializerOptions);
    }
    private static string CreateDescription(SceneConfiguration configuration, List<ISceneTool> tools)
    {
        if (!configuration.AutoGenerateToolDescription)
        {
            return configuration.Description; // Standard behavior - return configured description
        }

        var toolNames = tools.Select(t => t.Name).ToList();

        if (toolNames.Count == 0)
        {
            return configuration.Description; // No tools available, return original description
        }

        var toolList = $"Available tools: {string.Join(", ", toolNames)}";

        if (string.IsNullOrWhiteSpace(configuration.Description))
        {
            // No manual description - generate a default one
            return $"This scene provides the following capabilities: {string.Join(", ", toolNames)}";
        }

        // Manual description exists - append tool list
        var description = configuration.Description.TrimEnd();
        var separator = description.EndsWith('.') ? " " : ". ";
        return $"{description}{separator}{toolList}";
    }
    public AIFunction AiFunction { get; }
    public string Name => _config.Name;

    public string Description { get; }

    public IReadOnlyList<McpServerReference> McpServerReferences => _config.McpServerReferences;
    public IReadOnlyList<ClientInteractionDefinition>? ClientInteractionDefinitions => _config.ClientInteractionDefinitions;
    public TimeSpan CacheExpiration => _config.CacheExpiration;
    public async Task ExecuteActorsAsync(SceneContext context, CancellationToken cancellationToken)
    {
        var actorMessages = new List<string>();

        // Collect all actor messages
        foreach (var actor in Actors)
        {
            var response = await actor.PlayAsync(context, cancellationToken);

            if (!string.IsNullOrWhiteSpace(response.Message))
            {
                actorMessages.Add(response.Message);
            }
        }

        // Combine all actor messages into a single system message with bullet points
        if (actorMessages.Count > 0)
        {
            var combinedMessage = string.Join("\n", actorMessages.Select(msg => $"- {msg}"));
            context.AddSceneActorMessage(Name, "Combined", combinedMessage);
        }
    }
}
