namespace Rystem.PlayFramework;

/// <summary>
/// Settings for a specific scene request.
/// </summary>
public sealed class SceneRequestSettings
{
    /// <summary>
    /// Maximum recursion depth for planning (default: 5).
    /// </summary>
    public int MaxRecursionDepth { get; set; } = 5;

    /// <summary>
    /// Execution mode for this request.
    /// - Direct: Single scene, fast execution (default)
    /// - Planning: Multi-scene with upfront plan (requires IPlanner)
    /// - DynamicChaining: Multi-scene with live decisions
    /// If not set, uses the default configured in PlayFrameworkSettings.
    /// </summary>
    public SceneExecutionMode? ExecutionMode { get; set; }

    /// <summary>
    /// Whether to enable summarization for this request.
    /// </summary>
    public bool EnableSummarization { get; set; } = true;

    /// <summary>
    /// Whether to enable director for multi-scene orchestration.
    /// </summary>
    public bool EnableDirector { get; set; }

    /// <summary>
    /// Model to use for this request (overrides default).
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Temperature for LLM calls (0.0 - 2.0).
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Maximum tokens to generate.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Cache behavior for this request.
    /// </summary>
    public CacheBehavior CacheBehavior { get; set; } = CacheBehavior.Default;

    /// <summary>
    /// Maximum budget for this request (in the configured currency).
    /// If set, execution will stop when total cost exceeds this value.
    /// Set to null for unlimited budget (default).
    /// </summary>
    public decimal? MaxBudget { get; set; }

    /// <summary>
    /// Enable streaming for text responses.
    /// When enabled, text responses are streamed token-by-token for better UX.
    /// Note: Tool/function calls are never streamed (require complete JSON).
    /// </summary>
    public bool EnableStreaming { get; set; }

    /// <summary>
    /// Maximum number of scene executions in dynamic chaining mode.
    /// This is a safety limit to prevent infinite loops — the LLM may stop earlier
    /// via the continuation check. Scenes can be re-executed within this limit.
    /// Default: 10.
    /// </summary>
    public int MaxDynamicScenes { get; set; } = 10;

    /// <summary>
    /// Unique key for this conversation.
    /// Used by cache and memory to store and retrieve conversation-specific data.
    /// When resuming after AwaitingClient, pass this key along with ClientInteractionResults.
    /// If not provided, a new GUID will be generated (resulting in no cache/memory reuse).
    /// </summary>
    public string? ConversationKey { get; set; }

    /// <summary>
    /// Name of the scene to execute directly (used with SceneExecutionMode.Scene).
    /// Bypasses scene selection and executes the named scene directly.
    /// Also set automatically when resuming from continuation token.
    /// </summary>
    public string? SceneName { get; set; }

    /// <summary>
    /// Results from client-side tool executions.
    /// Used to resume execution after AwaitingClient status.
    /// </summary>
    public List<ClientInteractionResult>? ClientInteractionResults { get; set; }

    /// <summary>
    /// Optional per-scene tool constraints for the current request.
    /// When provided, only the matching tools are exposed for the specified scene.
    /// If exactly one matching tool is still pending, it is forced as the next tool call.
    /// </summary>
    public List<ForcedToolRequest>? ForcedTools { get; set; }

    /// <summary>
    /// Additional system-level instructions to append to the initial context.
    /// These are injected into the system prompt alongside main actor outputs,
    /// giving them high priority. Useful for voice pipeline language instructions
    /// or other per-request behavioral overrides.
    /// </summary>
    public List<string>? AdditionalSystemInstructions { get; set; }

    /// <summary>
    /// When <c>true</c>, the server injects a voice-style system instruction
    /// that tells the LLM to respond in a conversational, speech-friendly way
    /// (no tables, no markdown, no lists unless explicitly requested).
    /// Automatically set by the server-side <c>VoicePipeline</c> and can be
    /// set by clients using browser-side voice.
    /// </summary>
    public bool IsVoiceMode { get; set; }
}
