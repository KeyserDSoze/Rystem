using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Rystem.PlayFramework.Configuration;
using Rystem.PlayFramework.Domain.Wrappers;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework;

/// <summary>
/// Tool that represents a client-side interaction (browser/mobile execution).
/// This tool is used to inform the LLM about available client capabilities.
/// Actual execution is handled by ClientInteractionHandler.
/// </summary>
internal sealed class ClientInteractionTool : ISceneTool
{
    private readonly ClientInteractionDefinition _definition;

    public ClientInteractionTool(ClientInteractionDefinition definition)
    {
        _definition = definition;
        Name = _definition.ToolName;
        Description = _definition.Description ?? $"Client-side tool: {_definition.ToolName}";
        ToolDescription = ToAITool();
    }

    private AIFunctionDeclaration ToAITool()
    {
        JsonElement schema;

        if (!string.IsNullOrWhiteSpace(_definition.JsonSchema))
        {
            schema = JsonDocument.Parse(_definition.JsonSchema).RootElement;
            var declaration = AIFunctionFactory.CreateDeclaration(
                name: Name,
                description: Description,
                jsonSchema: schema
            );
            return declaration;
        }
        else
        {
            var declaration = AIFunctionFactory.Create(
            () => Name,
            Name,
            Description,
            JsonHelper.JsonSerializerOptions);
            return declaration;
        }
    }

    public string Name { get; }

    public string Description { get; }

    public AITool ToolDescription { get; }

    /// <summary>
    /// This method should never be called - client tools are intercepted by SceneExecutor.
    /// If called, it means there's a bug in the flow.
    /// </summary>
    public Task<object?> ExecuteAsync(
        string arguments,
        SceneContext context,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            $"Client tool '{Name}' cannot be executed on server. " +
            "This tool should be intercepted by ClientInteractionHandler in SceneExecutor. " +
            "If you see this error, there's a bug in the execution flow.");
    }
}
