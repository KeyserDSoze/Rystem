using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Telemetry;
using System.Diagnostics;

namespace Rystem.PlayFramework;

/// <summary>
/// Default director implementation (basic multi-scene orchestration).
/// </summary>
internal sealed class MainDirector : IDirector
{
    private readonly PlayFrameworkSettings _settings;
    private readonly ILogger<MainDirector> _logger;

    public MainDirector(PlayFrameworkSettings settings, ILogger<MainDirector> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public Task<DirectorResponse> DirectAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken = default)
    {
        using var activity = PlayFrameworkActivitySource.Instance.StartActivity(
            PlayFrameworkActivitySource.Activities.DirectorMakeDecision, ActivityKind.Internal);

        // Simple implementation: don't execute again by default
        // This can be enhanced with LLM-based decision making
        var response = new DirectorResponse
        {
            ExecuteAgain = false,
            Reasoning = "Single scene execution completed"
        };

        activity?.SetTag(PlayFrameworkActivitySource.Tags.DirectorDecision, response.Reasoning);
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.AddEvent(new ActivityEvent(PlayFrameworkActivitySource.Events.DirectorDecisionMade));

        _logger.LogDebug("Director decision: ExecuteAgain={ExecuteAgain}, Reasoning={Reasoning}",
            response.ExecuteAgain, response.Reasoning);

        return Task.FromResult(response);
    }
}
