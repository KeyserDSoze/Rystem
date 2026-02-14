using Rystem.PlayFramework;

namespace Rystem.PlayFramework.Services.Helpers;

/// <summary>
/// Implementation of scene matching helper for PlayFramework.
/// </summary>
internal sealed class SceneMatchingHelper : ISceneMatchingHelper
{
    /// <inheritdoc />
    public IScene? FindSceneByFuzzyMatch(string requestedName, ISceneFactory sceneFactory)
    {
        // Normalize scene names for matching
        var normalized = NormalizeSceneName(requestedName);

        foreach (var sceneName in sceneFactory.GetSceneNames())
        {
            if (NormalizeSceneName(sceneName) == normalized)
            {
                return sceneFactory.Create(sceneName);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public string NormalizeSceneName(string name)
    {
        return name.Replace("-", "")
                  .Replace("_", "")
                  .Replace(" ", "")
                  .ToLowerInvariant()
                  .Trim();
    }
}
