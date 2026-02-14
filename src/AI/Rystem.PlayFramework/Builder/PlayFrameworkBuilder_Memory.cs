using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for PlayFrameworkBuilder to configure conversation memory.
/// </summary>
public static class PlayFrameworkBuilder_Memory
{
    /// <summary>
    /// Enables conversation memory with persistence across requests.
    /// Memory automatically loads previous context at the start and saves updated context at the end.
    /// </summary>
    /// <param name="builder">PlayFramework builder.</param>
    /// <param name="configure">Memory configuration action.</param>
    /// <returns>Builder for chaining.</returns>
    public static PlayFrameworkBuilder WithMemory(
        this PlayFrameworkBuilder builder,
        Action<MemoryBuilder> configure)
    {
        var memoryBuilder = new MemoryBuilder();
        configure(memoryBuilder);

        // Enable memory
        memoryBuilder.Settings.Enabled = true;

        // Store in builder for DI registration
        builder.Settings.Memory = memoryBuilder.Settings;
        builder.HasCustomMemoryStorage = memoryBuilder.HasCustomStorage;
        builder.CustomMemoryStorageType = memoryBuilder.CustomStorageType;
        builder.HasCustomMemory = memoryBuilder.HasCustomMemory;
        builder.CustomMemoryType = memoryBuilder.CustomMemoryType;

        return builder;
    }
}
