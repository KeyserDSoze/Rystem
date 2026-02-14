using Rystem.PlayFramework;

namespace Rystem.PlayFramework.Services.Helpers;

/// <summary>
/// Helper service for scene name matching and normalization.
/// </summary>
internal interface ISceneMatchingHelper
{
    /// <summary>
    /// Finds a scene by fuzzy matching the requested name.
    /// Normalizes scene names by removing hyphens, underscores, spaces and converting to lowercase.
    /// </summary>
    /// <param name="requestedName">Requested scene name (may use different casing/separators).</param>
    /// <param name="sceneFactory">Factory to retrieve available scenes.</param>
    /// <returns>Matched scene or null if not found.</returns>
    IScene? FindSceneByFuzzyMatch(string requestedName, ISceneFactory sceneFactory);

    /// <summary>
    /// Normalizes a scene name for matching.
    /// Removes hyphens, underscores, spaces and converts to lowercase.
    /// </summary>
    /// <param name="name">Scene name to normalize.</param>
    /// <returns>Normalized scene name.</returns>
    string NormalizeSceneName(string name);
}
