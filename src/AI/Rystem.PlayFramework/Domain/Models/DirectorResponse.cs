namespace Rystem.PlayFramework;

/// <summary>
/// Response from the director after scene execution.
/// </summary>
public sealed class DirectorResponse
{
    /// <summary>
    /// Whether to execute another scene.
    /// </summary>
    public bool ExecuteAgain { get; set; }

    /// <summary>
    /// Scenes to exclude from next execution (loop prevention).
    /// </summary>
    public List<string> CutScenes { get; set; } = [];

    /// <summary>
    /// Reasoning for the decision.
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Specific scene to execute next (null = let AI choose).
    /// </summary>
    public string? NextScene { get; set; }
}
