using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RepositoryFramework;

namespace Rystem.PlayFramework;

/// <summary>
/// Repository-backed storage for conversation memory.
/// Uses <see cref="IRepository{StoredMemory, string}"/> to persist and retrieve <see cref="ConversationMemory"/> objects.
/// The user must register <c>IRepository&lt;StoredMemory, string&gt;</c> with any supported backend
/// (e.g. <c>services.AddRepository&lt;StoredMemory, string&gt;(name).WithInMemory()</c>).
/// </summary>
internal sealed class RepositoryMemoryStorage : IMemoryStorage
{
    private readonly IFactory<IRepository<StoredMemory, string>> _repositoryFactory;
    private readonly IFactory<MemorySettings> _settingsFactory;
    private readonly ILogger<RepositoryMemoryStorage> _logger;
    private IRepository<StoredMemory, string> _repository = null!;
    private MemorySettings? _settings;
    private string? _factoryName;

    public RepositoryMemoryStorage(
        ILogger<RepositoryMemoryStorage> logger,
        IFactory<IRepository<StoredMemory, string>> repositoryFactory,
        IFactory<MemorySettings> settingsFactory)
    {
        _logger = logger;
        _repositoryFactory = repositoryFactory;
        _settingsFactory = settingsFactory;
    }

    public bool FactoryNameAlreadySetup { get; set; }

    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        _factoryName = name?.ToString() ?? "default";
        _settings = _settingsFactory.Create(name);
        _repository = _repositoryFactory.Create(name)
            ?? throw new InvalidOperationException(
                $"IRepository<StoredMemory, string> not found for factory '{_factoryName}'. " +
                "Register it via services.AddRepository<StoredMemory, string>().WithInMemory() or another supported backend.");

        if (_settings?.StorageKeys != null && _settings.StorageKeys.Length > 0)
        {
            _logger.LogDebug("RepositoryMemoryStorage initialized with metadata-driven keys: {Keys} (Factory: {FactoryName})",
                string.Join(", ", _settings.StorageKeys), _factoryName);
        }
        else
        {
            _logger.LogDebug("RepositoryMemoryStorage initialized with ConversationKey-driven mode (Factory: {FactoryName})",
                _factoryName);
        }
    }

    public async Task<ConversationMemory?> GetAsync(
        string conversationKey,
        IReadOnlyDictionary<string, object>? metadata,
        SceneRequestSettings? settings,
        CancellationToken cancellationToken = default)
    {
        var key = BuildStorageKey(conversationKey, metadata);

        _logger.LogDebug("Retrieving memory for key: {Key} (Factory: {FactoryName})", key, _factoryName);

        var stored = await _repository.GetAsync(key, cancellationToken);

        if (stored != null)
        {
            _logger.LogInformation(
                "Memory found for key '{Key}': {ConversationCount} conversations, " +
                "last updated {LastUpdated}, summary length: {SummaryLength} chars (Factory: {FactoryName})",
                key, stored.ConversationCount, stored.LastUpdated, stored.Summary?.Length ?? 0, _factoryName);
        }
        else
        {
            _logger.LogDebug("No memory found for key: {Key} (Factory: {FactoryName})", key, _factoryName);
        }

        return stored?.ToConversationMemory();
    }

    public async Task SetAsync(
        string conversationKey,
        ConversationMemory memory,
        IReadOnlyDictionary<string, object>? metadata,
        SceneRequestSettings? settings,
        CancellationToken cancellationToken = default)
    {
        var key = BuildStorageKey(conversationKey, metadata);
        memory.LastUpdated = DateTime.UtcNow;

        var stored = StoredMemory.FromConversationMemory(key, memory);

        var existing = await _repository.GetAsync(key, cancellationToken);
        if (existing != null)
            await _repository.UpdateAsync(key, stored, cancellationToken);
        else
            await _repository.InsertAsync(key, stored, cancellationToken);

        _logger.LogInformation(
            "Memory saved for key '{Key}': {ConversationCount} conversations, " +
            "summary length: {SummaryLength} chars, facts: {FactCount} (Factory: {FactoryName})",
            key, memory.ConversationCount, memory.Summary?.Length ?? 0, memory.ImportantFacts.Count, _factoryName);
    }

    private string BuildStorageKey(string conversationKey, IReadOnlyDictionary<string, object>? metadata)
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
