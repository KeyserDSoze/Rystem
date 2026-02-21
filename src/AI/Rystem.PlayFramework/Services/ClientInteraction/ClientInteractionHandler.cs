using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Configuration;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework.Services;

/// <summary>
/// Implementation of client interaction handling.
/// </summary>
internal sealed class ClientInteractionHandler : IClientInteractionHandler
{
    private readonly ILogger<ClientInteractionHandler> _logger;

    public ClientInteractionHandler(ILogger<ClientInteractionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Checks if tool is registered as client-side and creates request.
    /// </summary>
    public ClientInteractionRequest? CreateRequestIfClientTool(
        IReadOnlyList<ClientInteractionDefinition> clientInteractionDefinitions,
        string toolName,
        Dictionary<string, object?>? arguments = null)
    {
        if (clientInteractionDefinitions == null || !clientInteractionDefinitions.Any())
        {
            _logger.LogDebug("No client interaction definitions registered");
            return null;
        }

        // Normalize the incoming tool name from LLM for matching
        var normalizedToolName = ToolNameNormalizer.Normalize(toolName);

        _logger.LogDebug("Checking if '{ToolName}' (normalized: '{NormalizedToolName}') is a client tool. " +
            "Registered client tools: [{ClientTools}]",
            toolName, normalizedToolName,
            string.Join(", ", clientInteractionDefinitions.Select(d => d.ToolName)));

        var definition = clientInteractionDefinitions
            .FirstOrDefault(d => d.ToolName.Equals(normalizedToolName, StringComparison.OrdinalIgnoreCase));

        if (definition == null)
        {
            _logger.LogDebug("Tool '{ToolName}' is NOT a client tool", toolName);
            return null;
        }

        var interactionId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Client tool '{ToolName}' detected. Creating interaction request {InteractionId}",
            toolName, interactionId);

        return new ClientInteractionRequest
        {
            InteractionId = interactionId,
            ToolName = definition.ToolName,
            Arguments = arguments,
            ArgumentsSchema = definition.JsonSchema,
            Description = definition.Description,
            TimeoutSeconds = definition.TimeoutSeconds
        };
    }

    /// <summary>
    /// Validates client interaction result.
    /// </summary>
    public bool ValidateResult(ClientInteractionResult result)
    {
        if (!string.IsNullOrEmpty(result.Error))
        {
            _logger.LogWarning(
                "Client interaction {InteractionId} failed: {Error}",
                result.InteractionId, result.Error);
            return false;
        }

        if (result.Contents == null || !result.Contents.Any())
        {
            _logger.LogWarning(
                "Client interaction {InteractionId} returned no contents",
                result.InteractionId);
            return false;
        }

        _logger.LogInformation(
            "Client interaction {InteractionId} completed successfully with {Count} contents",
            result.InteractionId, result.Contents.Count);

        return true;
    }
}
