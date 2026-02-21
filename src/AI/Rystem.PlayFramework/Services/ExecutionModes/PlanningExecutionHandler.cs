using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Rystem.PlayFramework.Services.ExecutionModes;

/// <summary>
/// Handles Planning execution mode - creates and executes a multi-step plan.
/// </summary>
internal sealed class PlanningExecutionHandler : IExecutionModeHandler
{
    private readonly IFactory<ExecutionModeHandlerDependencies> _dependenciesFactory;
    private readonly IFactory<ISceneExecutor> _sceneExecutorFactory;
    private readonly IFactory<FinalResponseGenerator> _finalResponseGeneratorFactory;
    private readonly IFactory<IPlanner> _plannerFactory;

    public PlanningExecutionHandler(
        IFactory<ExecutionModeHandlerDependencies> dependenciesFactory,
        IFactory<ISceneExecutor> sceneExecutorFactory,
        IFactory<FinalResponseGenerator> finalResponseGeneratorFactory,
        IFactory<IPlanner> plannerFactory)
    {
        _dependenciesFactory = dependenciesFactory;
        _sceneExecutorFactory = sceneExecutorFactory;
        _finalResponseGeneratorFactory = finalResponseGeneratorFactory;
        _plannerFactory = plannerFactory;
    }

    public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        AnyOf<string?, Enum>? factoryName,
        SceneContext context,
        SceneRequestSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Resolve dependencies from factory
        var dependencies = _dependenciesFactory.Create(factoryName)
            ?? throw new InvalidOperationException($"ExecutionModeHandlerDependencies not found for factory: {factoryName}");

        var sceneExecutor = _sceneExecutorFactory.Create(factoryName)
            ?? throw new InvalidOperationException($"SceneExecutor not found for factory: {factoryName}");

        var finalResponseGenerator = _finalResponseGeneratorFactory.Create(factoryName)
            ?? throw new InvalidOperationException($"FinalResponseGenerator not found for factory: {factoryName}");

        var planner = _plannerFactory.Create(factoryName);

        if (planner == null)
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

        var plan = await planner.CreatePlanAsync(context, settings, cancellationToken);
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
            await foreach (var response in ExecutePlanAsync(dependencies, sceneExecutor, finalResponseGenerator, context, settings, plan, cancellationToken))
            {
                yield return response;
            }
        }
    }

    private async IAsyncEnumerable<AiSceneResponse> ExecutePlanAsync(
        ExecutionModeHandlerDependencies dependencies,
        ISceneExecutor sceneExecutor,
        FinalResponseGenerator finalResponseGenerator,
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
            var scene = dependencies.SceneFactory.TryGetScene(step.SceneName);
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
            await foreach (var response in sceneExecutor.ExecuteSceneAsync(context, scene, settings, cancellationToken))
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
            await foreach (var response in finalResponseGenerator.GenerateAsync(context, settings, cancellationToken))
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
