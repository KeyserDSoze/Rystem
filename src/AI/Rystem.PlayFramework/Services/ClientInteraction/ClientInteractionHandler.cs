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
            return null;

        // Normalize the incoming tool name from LLM for matching
        var normalizedToolName = ToolNameNormalizer.Normalize(toolName);

        var definition = clientInteractionDefinitions
            .FirstOrDefault(d => d.ToolName.Equals(normalizedToolName, StringComparison.OrdinalIgnoreCase));

        if (definition == null)
            return null;

        var interactionId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Client tool '{ToolName}' detected. Creating interaction request {InteractionId}",
            toolName, interactionId);

        return new ClientInteractionRequest
        {
            InteractionId = interactionId,
            ToolName = definition.ToolName,
            Arguments = arguments,
            ArgumentsSchema = definition.ArgumentsSchema,
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
