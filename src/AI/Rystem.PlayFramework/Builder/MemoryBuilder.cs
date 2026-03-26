namespace Rystem.PlayFramework;

using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework;

/// <summary>
/// Fluent API builder for configuring conversation memory.
/// </summary>
public sealed class MemoryBuilder
{
    private readonly MemorySettings _settings = new();
    private Type? _customStorageType;
    private Type? _customMemoryType;

    internal MemorySettings Settings => _settings;
    internal Type? CustomStorageType => _customStorageType;
    internal Type? CustomMemoryType => _customMemoryType;
    internal bool HasCustomStorage => _customStorageType != null;
    internal bool HasCustomMemory => _customMemoryType != null;
    internal Action<string?, IRepositoryBuilder<StoredMemory, string>>? RepositoryStorageConfigure { get; private set; }

    /// <summary>
    /// Use default in-memory storage with metadata-based keys (similar to rate limiting GroupBy).
    /// Example: .WithDefaultMemoryStorage("userId", "sessionId")
    /// Creates storage keys like "userId:john|sessionId:abc123"
    /// </summary>
    /// <param name="metadataKeys">Metadata keys to combine for storage key.</param>
    public MemoryBuilder WithDefaultMemoryStorage(params string[] metadataKeys)
    {
        if (metadataKeys == null || metadataKeys.Length == 0)
            throw new ArgumentException("At least one metadata key required for WithDefaultMemoryStorage. Example: .WithDefaultMemoryStorage(\"userId\", \"sessionId\")");

        _settings.StorageKeys = metadataKeys;
        return this;
    }

    /// <summary>
    /// Use custom storage implementation (e.g., Redis, SQL, CosmosDB).
    /// Custom storage can implement its own key generation logic.
    /// </summary>
    public MemoryBuilder WithCustomStorage<TStorage>()
        where TStorage : IMemoryStorage
    {
        _customStorageType = typeof(TStorage);
        return this;
    }

    /// <summary>
    /// Use custom memory implementation.
    /// </summary>
    public MemoryBuilder WithCustomMemory<TMemory>()
        where TMemory : IMemory
    {
        _customMemoryType = typeof(TMemory);
        return this;
    }

    /// <summary>
    /// Set maximum length for conversation summary.
    /// Default: 2000 characters.
    /// </summary>
    public MemoryBuilder WithMaxSummaryLength(int maxLength)
    {
        if (maxLength <= 0)
            throw new ArgumentException("Max summary length must be > 0", nameof(maxLength));

        _settings.MaxSummaryLength = maxLength;
        return this;
    }

    /// <summary>
    /// Set custom system prompt for memory summarization.
    /// </summary>
    public MemoryBuilder WithSystemPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("System prompt cannot be empty", nameof(prompt));

        _settings.SystemPrompt = prompt;
        return this;
    }

    /// <summary>
    /// Whether to include previous memory in summarization prompts.
    /// Default: true.
    /// </summary>
    public MemoryBuilder WithIncludePreviousMemory(bool include)
    {
        _settings.IncludePreviousMemory = include;
        return this;
    }

    /// <summary>
    /// Configures the <see cref="IRepository{StoredMemory, string}"/> backend used to persist memory
    /// inline using the supplied action. The PlayFramework factory name is automatically injected so
    /// backends resolve correctly, e.g.:
    /// <code>
    /// .WithMemory(m => m.UseRepository((name, repo) => repo.WithInMemory(name: name)))
    /// </code>
    /// When this method is called the default <see cref="RepositoryMemoryStorage"/> implementation
    /// is used (which requires the repository to be configured here or registered separately).
    /// </summary>
    public MemoryBuilder UseRepository(
        Action<string?, IRepositoryBuilder<StoredMemory, string>> configure)
    {
        RepositoryStorageConfigure = configure;
        return this;
    }

    /// <summary>
    /// Uses the <see cref="IRepository{StoredMemory, string}"/> that is already registered
    /// externally (e.g. via <c>services.AddRepository&lt;StoredMemory, string&gt;(...)</c>
    /// in Program.cs) with the same factory name as the PlayFramework instance.
    /// Mirrors the behaviour of the parameterless <c>PlayFrameworkBuilder.UseRepository()</c>
    /// for conversations: no inline configuration — the repository is expected to exist.
    /// </summary>
    public MemoryBuilder UseRepository()
    {
        // RepositoryStorageConfigure stays null → PlayFrameworkBuilder_Memory skips inline
        // registration → ServiceCollectionExtensions wires RepositoryMemoryStorage against
        // the externally-registered IRepository<StoredMemory, string>.
        return this;
    }
}
