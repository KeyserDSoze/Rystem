using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Factory for creating scene selection tools.
/// </summary>
internal static class SceneSelectionToolFactory
{
    public static AIFunction CreateSceneSelectionTool(IScene scene)
    {
        return AIFunctionFactory.Create(
            (string input) => scene.Name,
            scene.Name,
            scene.Description);
    }
}
