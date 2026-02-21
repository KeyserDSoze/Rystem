namespace Rystem.PlayFramework;

/// <summary>
/// High-level service for creating and managing scene managers with factory pattern.
/// Wraps IFactory&lt;ISceneManager&gt; with a cleaner API.
/// </summary>
public interface IPlayFramework
{
    /// <summary>
    /// Creates a scene manager for the specified configuration key.
    /// </summary>
    /// <param name="name">Configuration key (string or enum). If null, uses default configuration.</param>
    /// <returns>Scene manager instance, or null if configuration not found.</returns>
    ISceneManager? Create(AnyOf<string?, Enum>? name = null);

    /// <summary>
    /// Creates a scene manager for the specified configuration key, if it doesn't exist, use the default scene manager (the first created in your setup)
    /// </summary>
    /// <param name="name">Configuration key (string or enum). If null, uses default configuration.</param>
    /// <returns>Scene manager instance, or null if configuration not found.</returns>
    ISceneManager? CreateOrDefault(AnyOf<string?, Enum>? name = null);

    /// <summary>
    /// Checks if a configuration exists for the specified key.
    /// </summary>
    /// <param name="name">Configuration key (string or enum).</param>
    /// <returns>True if the configuration exists; otherwise, false.</returns>
    bool Exists(AnyOf<string?, Enum>? name = null);
}
