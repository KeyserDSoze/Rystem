using System.Runtime.CompilerServices;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Handles Planning execution mode - creates and executes a multi-step plan.
/// </summary>
internal sealed class PlanningExecutionHandler : IExecutionModeHandler
{
    private readonly ExecutionModeHandlerDependencies _dependencies;
    private readonly ISceneExecutor _sceneExecutor;
    private readonly FinalResponseGenerator _finalResponseGenerator;
    private readonly IPlanner? _planner;

    public PlanningExecutionHandler(
        ExecutionModeHandlerDependencies dependencies,
        ISceneExecutor sceneExecutor,
        FinalResponseGenerator finalResponseGenerator,
        IPlanner? planner)
    {
        _dependencies = dependencies;
        _sceneExecutor = sceneExecutor;
        _finalResponseGenerator = finalResponseGenerator;
        _planner = planner;
    }

    public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        SceneContext context,
        SceneRequestSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_planner == null)
        {
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.Error,
                ErrorMessage = "Planning mode requested but no IPlanner is registered"
            });

            yield break;
        }

        // Create execution plan
        yield return YieldStatus(AiResponseStatus.Planning, "Creating execution plan");

        var plan = await _planner.CreatePlanAsync(context, settings, cancellationToken);
        context.ExecutionPlan = plan;

        if (!plan.NeedsExecution)
        {
            // Direct answer available
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.Running,
                Message = plan.Reasoning
            });
        }
        else
        {
            // Execute plan
            await foreach (var response in ExecutePlanAsync(context, settings, plan, cancellationToken))
            {
                yield return response;
            }
        }
    }

    private async IAsyncEnumerable<AiSceneResponse> ExecutePlanAsync(
        SceneContext context,
        SceneRequestSettings settings,
        ExecutionPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        int recursionDepth = 0)
    {
        if (recursionDepth >= settings.MaxRecursionDepth)
        {
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.Error,
                ErrorMessage = $"Maximum recursion depth ({settings.MaxRecursionDepth}) reached"
            });
            context.ExecutionPhase = ExecutionPhase.Completed;
            yield break;
        }

        // Execute each step in order
        foreach (var step in plan.Steps.OrderBy(s => s.StepNumber))
        {
            if (step.IsCompleted)
            {
                continue;
            }

            yield return YieldStatus(AiResponseStatus.ExecutingScene, $"Executing step {step.StepNumber}: {step.SceneName}");

            // Execute scene for this step
            var scene = _dependencies.SceneMatchingHelper.FindSceneByFuzzyMatch(step.SceneName, _dependencies.SceneFactory);
            if (scene == null)
            {
                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.Error,
                    SceneName = step.SceneName,
                    ErrorMessage = $"Scene '{step.SceneName}' not found"
                });
                continue;
            }

            // Execute scene
            await foreach (var response in _sceneExecutor.ExecuteSceneAsync(context, scene, settings, cancellationToken))
            {
                yield return response;
            }

            step.IsCompleted = true;
        }

        // Check if we need to continue (all steps completed? can we answer now?)
        var allStepsCompleted = plan.Steps.All(s => s.IsCompleted);
        if (allStepsCompleted)
        {
            // Generate final response
            await foreach (var response in _finalResponseGenerator.GenerateAsync(context, settings, cancellationToken))
            {
                yield return response;
            }
        }
    }

    private static AiSceneResponse YieldStatus(AiResponseStatus status, string? message = null)
    {
        return new AiSceneResponse
        {
            Status = status,
            Message = message
        };
    }

    private static AiSceneResponse YieldAndTrack(SceneContext context, AiSceneResponse response)
    {
        response.TotalCost = context.TotalCost;
        context.Responses.Add(response);
        return response;
    }
}
