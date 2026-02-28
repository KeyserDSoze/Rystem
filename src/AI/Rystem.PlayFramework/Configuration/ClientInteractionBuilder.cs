using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework.Configuration;

/// <summary>
/// Builder for configuring client-side tool interactions.
/// Used within scene.OnClient() to register tools that execute in browser/mobile app.
/// </summary>
public sealed class ClientInteractionBuilder
{
    private readonly List<ClientInteractionDefinition> _definitions = [];

    /// <summary>
    /// Registers a tool with strongly-typed arguments.
    /// JSON Schema is automatically generated from type T using System.Text.Json.Schema.
    /// Supports [Description], [Required], and other standard attributes.
    /// </summary>
    /// <typeparam name="T">Argument model type (must be a class)</typeparam>
    /// <param name="toolName">Unique tool name (e.g., "CapturePhoto")</param>
    /// <param name="description">Human-readable description of what this tool does</param>
    /// <param name="timeoutSeconds">Maximum time client has to execute and return result</param>
    /// <returns>Builder for fluent configuration</returns>
    public ClientInteractionBuilder AddTool<T>(
        string toolName,
        string? description = null,
        int timeoutSeconds = 30) where T : class
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be empty", nameof(toolName));

        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));
        var schemaNode = JsonSchemaExporter.GetJsonSchemaAsNode(JsonHelper.JsonSerializerOptions, typeof(T));
        var jsonSchema = schemaNode.ToJsonString(JsonHelper.JsonSerializerOptions);
        _definitions.Add(new ClientInteractionDefinition
        {
            ToolName = ToolNameNormalizer.Normalize(toolName),
            Description = description,
            TimeoutSeconds = timeoutSeconds,
            ArgumentType = typeof(T),
            JsonSchema = jsonSchema
        });

        return this;
    }

    /// <summary>
    /// Registers a simple tool without arguments.
    /// Use this for tools that don't need parameters (e.g., "PlaySound", "Vibrate").
    /// </summary>
    /// <param name="toolName">Unique tool name</param>
    /// <param name="description">Human-readable description</param>
    /// <param name="timeoutSeconds">Maximum execution time</param>
    /// <returns>Builder for fluent configuration</returns>
    public ClientInteractionBuilder AddTool(
        string toolName,
        string? description = null,
        int timeoutSeconds = 30)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be empty", nameof(toolName));

        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

        _definitions.Add(new ClientInteractionDefinition
        {
            ToolName = ToolNameNormalizer.Normalize(toolName),
            Description = description,
            TimeoutSeconds = timeoutSeconds
        });

        return this;
    }

    /// <summary>
    /// Registers a simple tool without arguments.
    /// Use this for tools that don't need parameters (e.g., "PlaySound", "Vibrate").
    /// </summary>
    /// <param name="toolName">Unique tool name</param>
    /// <param name="jsonSchema">Json schema for input</param>
    /// <param name="description">Human-readable description</param>
    /// <param name="timeoutSeconds">Maximum execution time</param>
    /// <returns>Builder for fluent configuration</returns>
    public ClientInteractionBuilder AddTool(
        string toolName,
        string jsonSchema,
        string? description = null,
        int timeoutSeconds = 30)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be empty", nameof(toolName));

        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

        _definitions.Add(new ClientInteractionDefinition
        {
            ToolName = ToolNameNormalizer.Normalize(toolName),
            Description = description,
            TimeoutSeconds = timeoutSeconds,
            JsonSchema = jsonSchema
        });

        return this;
    }

    /// <summary>
    /// Registers a command (fire-and-forget tool) with strongly-typed arguments.
    /// Commands don't require immediate response - they are auto-completed with 'true' on next user message.
    /// Client can optionally send CommandResult (success + message) based on feedbackMode.
    /// Timeout protects against client-side execution failures (crash, hang, network error).
    /// </summary>
    /// <typeparam name="T">Argument model type (must be a class)</typeparam>
    /// <param name="toolName">Unique command name (e.g., "LogAction", "TrackEvent")</param>
    /// <param name="description">Human-readable description of what this command does</param>
    /// <param name="feedbackMode">When to send feedback (Never, OnError, Always)</param>
    /// <param name="timeoutSeconds">Maximum time for client-side execution (default: 30s)</param>
    /// <returns>Builder for fluent configuration</returns>
    public ClientInteractionBuilder AddCommand<T>(
        string toolName,
        string? description = null,
        CommandFeedbackMode feedbackMode = CommandFeedbackMode.OnError,
        int timeoutSeconds = 30) where T : class
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Command name cannot be empty", nameof(toolName));

        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

        var schemaNode = JsonSchemaExporter.GetJsonSchemaAsNode(JsonHelper.JsonSerializerOptions, typeof(T));
        var jsonSchema = schemaNode.ToJsonString(JsonHelper.JsonSerializerOptions);

        _definitions.Add(new ClientInteractionDefinition
        {
            ToolName = ToolNameNormalizer.Normalize(toolName),
            Description = description,
            TimeoutSeconds = timeoutSeconds,
            ArgumentType = typeof(T),
            JsonSchema = jsonSchema,
            IsCommand = true,
            FeedbackMode = feedbackMode
        });

        return this;
    }

    /// <summary>
    /// Registers a simple command without arguments.
    /// Commands are fire-and-forget - client can execute without sending immediate response.
    /// Timeout protects against client-side execution failures.
    /// </summary>
    /// <param name="toolName">Unique command name</param>
    /// <param name="description">Human-readable description</param>
    /// <param name="feedbackMode">When to send feedback (Never, OnError, Always)</param>
    /// <param name="timeoutSeconds">Maximum time for client-side execution</param>
    /// <returns>Builder for fluent configuration</returns>
    public ClientInteractionBuilder AddCommand(
        string toolName,
        string? description = null,
        CommandFeedbackMode feedbackMode = CommandFeedbackMode.OnError,
        int timeoutSeconds = 30)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Command name cannot be empty", nameof(toolName));

        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

        _definitions.Add(new ClientInteractionDefinition
        {
            ToolName = ToolNameNormalizer.Normalize(toolName),
            Description = description,
            TimeoutSeconds = timeoutSeconds,
            IsCommand = true,
            FeedbackMode = feedbackMode
        });

        return this;
    }

    /// <summary>
    /// Registers a command with manual JSON schema.
    /// Timeout protects against client-side execution failures.
    /// </summary>
    /// <param name="toolName">Unique command name</param>
    /// <param name="jsonSchema">JSON Schema for command arguments</param>
    /// <param name="description">Human-readable description</param>
    /// <param name="feedbackMode">When to send feedback (Never, OnError, Always)</param>
    /// <param name="timeoutSeconds">Maximum time for client-side execution</param>
    /// <returns>Builder for fluent configuration</returns>
    public ClientInteractionBuilder AddCommand(
        string toolName,
        string jsonSchema,
        string? description = null,
        CommandFeedbackMode feedbackMode = CommandFeedbackMode.OnError,
        int timeoutSeconds = 30)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Command name cannot be empty", nameof(toolName));

        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

        _definitions.Add(new ClientInteractionDefinition
        {
            ToolName = ToolNameNormalizer.Normalize(toolName),
            Description = description,
            TimeoutSeconds = timeoutSeconds,
            JsonSchema = jsonSchema,
            IsCommand = true,
            FeedbackMode = feedbackMode
        });

        return this;
    }

    /// <summary>
    /// Builds the final list of client interaction definitions.
    /// Called internally by SceneBuilder.
    /// </summary>
    internal IReadOnlyList<ClientInteractionDefinition> Build() => _definitions.AsReadOnly();
}
