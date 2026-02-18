using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Factory for creating scene selection tools.
/// </summary>
internal static class SceneSelectionToolFactory
{
    public static AITool CreateSceneSelectionTool(IScene scene)
    {
        return AIFunctionFactory.Create(
            () => scene.Name,
            scene.Name,
            scene.Description);
    }
}
