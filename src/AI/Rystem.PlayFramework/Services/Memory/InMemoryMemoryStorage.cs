using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Rystem.PlayFramework;

/// <summary>
/// In-memory storage for conversation memory.
/// Supports both metadata-driven keys (like rate limiting) and ConversationKey-driven storage.
/// </summary>
internal sealed class InMemoryMemoryStorage : IMemoryStorage
{
    private readonly ConcurrentDictionary<string, ConversationMemory> _storage = new();
    private readonly ILogger<InMemoryMemoryStorage> _logger;
    private readonly IFactory<MemorySettings> _settingsFactory;
    private MemorySettings? _settings;
    private string? _factoryName;

    public InMemoryMemoryStorage(
        ILogger<InMemoryMemoryStorage> logger,
        IFactory<MemorySettings> settingsFactory)
    {
        _logger = logger;
        _settingsFactory = settingsFactory;
    }
    public bool FactoryNameAlreadySetup { get; set; }
    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        _factoryName = name?.ToString() ?? "default";
        _settings = _settingsFactory.Create(name);

        if (_settings?.StorageKeys != null && _settings.StorageKeys.Length > 0)
        {
            _logger.LogDebug("InMemoryMemoryStorage initialized with metadata-driven keys: {Keys} (Factory: {FactoryName})",
                string.Join(", ", _settings.StorageKeys), _factoryName);
        }
        else
        {
            _logger.LogDebug("InMemoryMemoryStorage initialized with ConversationKey-driven mode (Factory: {FactoryName})",
                _factoryName);
        }
    }

    public Task<ConversationMemory?> GetAsync(
        string conversationKey,
        IReadOnlyDictionary<string, object>? metadata,
        SceneRequestSettings? settings,
        CancellationToken cancellationToken = default)
    {
        var key = BuildStorageKey(conversationKey, metadata, settings);

        _logger.LogDebug("Retrieving memory for key: {Key} (Factory: {FactoryName})", key, _factoryName);

        _storage.TryGetValue(key, out var memory);

        if (memory != null)
        {
            _logger.LogInformation(
                "Memory found for key '{Key}': {ConversationCount} conversations, " +
                "last updated {LastUpdated}, summary length: {SummaryLength} chars (Factory: {FactoryName})",
                key, memory.ConversationCount, memory.LastUpdated, memory.Summary?.Length ?? 0, _factoryName);
        }
        else
        {
            _logger.LogDebug("No memory found for key: {Key} (Factory: {FactoryName})", key, _factoryName);
        }

        return Task.FromResult(memory);
    }

    public Task SetAsync(
        string conversationKey,
        ConversationMemory memory,
        IReadOnlyDictionary<string, object>? metadata,
        SceneRequestSettings? settings,
        CancellationToken cancellationToken = default)
    {
        var key = BuildStorageKey(conversationKey, metadata, settings);

        memory.LastUpdated = DateTime.UtcNow;
        _storage[key] = memory;

        _logger.LogInformation(
            "Memory saved for key '{Key}': {ConversationCount} conversations, " +
            "summary length: {SummaryLength} chars, facts: {FactCount} (Factory: {FactoryName})",
            key, memory.ConversationCount, memory.Summary?.Length ?? 0, memory.ImportantFacts.Count, _factoryName);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Builds storage key based on configuration:
    /// 1. If StorageKeys configured → use metadata (userId:val1|sessionId:val2)
    /// 2. Otherwise → use conversationKey from settings
    /// </summary>
    private string BuildStorageKey(
        string conversationKey,
        IReadOnlyDictionary<string, object>? metadata,
        SceneRequestSettings? settings)
    {
        // Mode 1: Metadata-driven (like rate limiting GroupBy)
        if (_settings?.StorageKeys != null && _settings.StorageKeys.Length > 0)
        {
            var keyParts = new List<string>();

            foreach (var key in _settings.StorageKeys)
            {
                if (metadata?.TryGetValue(key, out var value) == true && value != null)
                {
                    keyParts.Add($"{key}:{value}");
                }
                else
                {
                    _logger.LogWarning(
                        "Metadata key '{Key}' not found in request metadata. Using 'unknown' for memory storage. " +
                        "Pass metadata dictionary with required keys in ExecuteAsync(message, metadata, settings). (Factory: {FactoryName})",
                        key, _factoryName);
                    keyParts.Add($"{key}:unknown");
                }
            }

            var compositeKey = string.Join("|", keyParts);
            _logger.LogDebug("Built memory storage key: {Key} from metadata keys: {Keys} (Factory: {FactoryName})",
                compositeKey, string.Join(", ", _settings.StorageKeys), _factoryName);
            return compositeKey;
        }

        // Mode 2: ConversationKey-driven (simple mode)
        _logger.LogDebug("Using conversationKey for memory storage: {Key} (Factory: {FactoryName})",
            conversationKey, _factoryName);
        return conversationKey;
    }
}
