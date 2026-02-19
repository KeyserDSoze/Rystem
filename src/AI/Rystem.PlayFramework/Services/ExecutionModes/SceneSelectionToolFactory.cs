using Microsoft.Extensions.AI;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Factory for creating scene selection tools.
/// </summary>
internal static class SceneSelectionToolFactory
{
    public static AITool CreateSceneSelectionTool(IScene scene)
    {
        // Normalize scene name to comply with OpenAI pattern ^[a-zA-Z0-9_\.-]+$
        // E.g., "General Requests" → "General_Requests"
        return AIFunctionFactory.Create(
            () => scene.Name,
            ToolNameNormalizer.Normalize(scene.Name),
            scene.Description);
    }
}
