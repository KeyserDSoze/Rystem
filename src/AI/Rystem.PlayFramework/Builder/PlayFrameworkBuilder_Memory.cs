using Microsoft.Extensions.DependencyInjection;
using RepositoryFramework;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for PlayFrameworkBuilder to configure conversation memory.
/// </summary>
public static class PlayFrameworkBuilder_Memory
{
    /// <summary>
    /// Enables conversation memory backed by a repository.
    /// Registers <see cref="IRepository{StoredMemory, string}"/> using the supplied action
    /// (the PlayFramework factory name is injected automatically) and wires up
    /// <see cref="RepositoryMemoryStorage"/> as the default storage implementation.
    /// Example:
    /// <code>
    /// builder.WithMemory((name, repo) => repo.WithInMemory(name: name))
    /// </code>
    /// </summary>
    public static PlayFrameworkBuilder WithMemory(
        this PlayFrameworkBuilder builder,
        Action<string?, IRepositoryBuilder<StoredMemory, string>> configureRepository)
    {
        var name = builder.Name?.ToString();
        builder.Services.AddRepository<StoredMemory, string>(
            repoBuilder => configureRepository(name, repoBuilder));

        builder.Settings.Memory = new MemorySettings { Enabled = true };
        // HasCustomMemoryStorage stays false → ServiceCollectionExtensions registers RepositoryMemoryStorage
        return builder;
    }

    /// <summary>
    /// Enables conversation memory with advanced settings (max summary length, system prompt, etc.).
    /// Call <c>.WithRepositoryStorage</c> inside the <paramref name="configure"/> action to
    /// configure the backing repository inline.
    /// </summary>
    public static PlayFrameworkBuilder WithMemory(
        this PlayFrameworkBuilder builder,
        Action<MemoryBuilder> configure)
    {
        var memoryBuilder = new MemoryBuilder();
        configure(memoryBuilder);

        memoryBuilder.Settings.Enabled = true;

        if (memoryBuilder.RepositoryStorageConfigure != null)
        {
            var name = builder.Name?.ToString();
            builder.Services.AddRepository<StoredMemory, string>(
                repoBuilder => memoryBuilder.RepositoryStorageConfigure(name, repoBuilder));
        }

        builder.Settings.Memory = memoryBuilder.Settings;
        builder.HasCustomMemoryStorage = memoryBuilder.HasCustomStorage;
        builder.CustomMemoryStorageType = memoryBuilder.CustomStorageType;
        builder.HasCustomMemory = memoryBuilder.HasCustomMemory;
        builder.CustomMemoryType = memoryBuilder.CustomMemoryType;

        return builder;
    }
}
