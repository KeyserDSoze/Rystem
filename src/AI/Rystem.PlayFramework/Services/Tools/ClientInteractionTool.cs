using Microsoft.Extensions.AI;
using Rystem.PlayFramework.Configuration;
using Rystem.PlayFramework.Helpers;
using System.Text.Json;
using System.Text.Json.Nodes;

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
    }

    public string Name => _definition.ToolName;

    public string Description => _definition.Description ?? $"Client-side tool: {_definition.ToolName}";

    public AITool ToAITool()
    {
        // If there's a JSON schema, use it to create the function with proper parameters
        if (!string.IsNullOrWhiteSpace(_definition.ArgumentsSchema))
        {
            // Parse the JSON schema to extract parameter information
            var schemaNode = JsonNode.Parse(_definition.ArgumentsSchema);

            // Create AIFunction with schema-based metadata
            // The LLM will see this tool with its proper argument structure
            string ClientToolStub(Dictionary<string, object?> args)
            {
                throw new InvalidOperationException(
                    $"Client tool '{Name}' should not be executed on server. This is handled by ClientInteractionHandler.");
            }

            return AIFunctionFactory.Create(
                ClientToolStub,
                name: Name,
                description: Description);
        }
        else
        {
            // No arguments - simple tool
            string ClientToolStubNoArgs()
            {
                throw new InvalidOperationException(
                    $"Client tool '{Name}' should not be executed on server. This is handled by ClientInteractionHandler.");
            }

            return AIFunctionFactory.Create(
                ClientToolStubNoArgs,
                name: Name,
                description: Description);
        }
    }

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
