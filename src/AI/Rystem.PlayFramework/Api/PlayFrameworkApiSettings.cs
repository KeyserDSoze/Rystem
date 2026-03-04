namespace Rystem.PlayFramework.Api;

/// <summary>
/// Settings for PlayFramework HTTP API endpoints.
/// </summary>
public sealed class PlayFrameworkApiSettings
{
    /// <summary>
    /// Base path for PlayFramework endpoints.
    /// Default: "/playframework"
    /// Example: "/api/ai" would result in "/api/ai/{factoryName}" and "/api/ai/{factoryName}/streaming"
    /// </summary>
    public string BasePath { get; set; } = "/playframework";

    /// <summary>
    /// Enable response compression for streaming endpoints.
    /// Default: true
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Maximum request body size in bytes (for multi-modal content).
    /// Default: 10MB
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 10_485_760; // 10MB

    /// <summary>
    /// Enable automatic metadata extraction from HTTP context (userId from claims, IP address, etc.).
    /// Default: true
    /// </summary>
    public bool EnableAutoMetadata { get; set; } = true;

    /// <summary>
    /// Require authentication for all endpoints.
    /// Default: false
    /// </summary>
    public bool RequireAuthentication { get; set; } = false;

    /// <summary>
    /// Authorization policies to apply to all endpoints.
    /// Example: ["ReadAccess", "PlayFrameworkUser"]
    /// </summary>
    public List<string> AuthorizationPolicies { get; set; } = new();

    /// <summary>
    /// Factory-specific authorization policies.
    /// Example: { "premium": ["PremiumUser"], "admin": ["AdminOnly"] }
    /// If specified, these policies are applied IN ADDITION to global policies.
    /// </summary>
    public Dictionary<string, List<string>> FactoryPolicies { get; set; } = new();

    /// <summary>
    /// Default factory name when not specified in URL (optional).
    /// If null, factory name is always required in URL.
    /// </summary>
    public string? DefaultFactoryName { get; set; }

    /// <summary>
    /// Enable conversation management endpoints (List, Get, Delete, Update Visibility).
    /// Requires IRepository&lt;StoredConversation, string&gt; to be registered.
    /// Default: false
    /// </summary>
    public bool EnableConversationEndpoints { get; set; } = false;

    /// <summary>
    /// Maximum number of conversations returned in a single List request.
    /// Default: 100
    /// </summary>
    public int MaxConversationsPageSize { get; set; } = 100;

    /// <summary>
    /// Enable voice pipeline endpoints (audio → STT → PlayFramework → TTS → audio streaming).
    /// Requires <c>.WithVoice()</c> to be configured on the PlayFramework builder
    /// and an <see cref="IVoiceAdapter"/> to be registered.
    /// Default: false
    /// </summary>
    public bool EnableVoiceEndpoints { get; set; } = false;

    /// <summary>
    /// Maximum audio upload size in bytes for voice endpoints.
    /// Default: 25MB (OpenAI Whisper limit).
    /// </summary>
    public long MaxAudioUploadSize { get; set; } = 26_214_400; // 25MB
}
