using Rystem.PlayFramework.Configuration;

namespace Rystem.PlayFramework.Services;

/// <summary>
/// Handles client interaction lifecycle: checking for client tools, generating requests, validating results.
/// </summary>
internal interface IClientInteractionHandler
{
    /// <summary>
    /// Checks if the given tool name is registered as a client-side tool in the scene.
    /// If yes, creates a ClientInteractionRequest to send to client.
    /// </summary>
    /// <param name="clientInteractionDefinitions">Client tool definitions from scene.</param>
    /// <param name="toolName">Tool name requested by LLM.</param>
    /// <param name="arguments">Arguments from LLM (if any).</param>
    /// <returns>ClientInteractionRequest if tool is client-side, null otherwise.</returns>
    ClientInteractionRequest? CreateRequestIfClientTool(
        IReadOnlyList<ClientInteractionDefinition> clientInteractionDefinitions,
        string toolName,
        Dictionary<string, object?>? arguments = null);

    /// <summary>
    /// Validates client interaction result and extracts AIContent[].
    /// </summary>
    /// <param name="result">Result from client execution.</param>
    /// <returns>True if valid, false if error.</returns>
    bool ValidateResult(ClientInteractionResult result);
}
