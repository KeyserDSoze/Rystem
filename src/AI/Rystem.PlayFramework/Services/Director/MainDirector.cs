namespace Rystem.PlayFramework;

/// <summary>
/// Default director implementation (basic multi-scene orchestration).
/// </summary>
internal sealed class MainDirector : IDirector
{
    private readonly PlayFrameworkSettings _settings;

    public MainDirector(PlayFrameworkSettings settings)
    {
        _settings = settings;
    }

    public Task<DirectorResponse> DirectAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken = default)
    {
        // Simple implementation: don't execute again by default
        // This can be enhanced with LLM-based decision making
        
        var response = new DirectorResponse
        {
            ExecuteAgain = false,
            Reasoning = "Single scene execution completed"
        };

        return Task.FromResult(response);
    }
}
