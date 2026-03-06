using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<PlanningExecutionHandler> _logger;

    public PlanningExecutionHandler(
        IFactory<ExecutionModeHandlerDependencies> dependenciesFactory,
        IFactory<ISceneExecutor> sceneExecutorFactory,
        IFactory<FinalResponseGenerator> finalResponseGeneratorFactory,
        IFactory<IPlanner> plannerFactory,
        ILogger<PlanningExecutionHandler> logger)
    {
        _dependenciesFactory = dependenciesFactory;
        _sceneExecutorFactory = sceneExecutorFactory;
        _finalResponseGeneratorFactory = finalResponseGeneratorFactory;
        _plannerFactory = plannerFactory;
        _logger = logger;
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

        var factoryNameString = factoryName?.ToString() ?? "default";
        _logger.LogDebug("Starting Planning execution mode (Factory: {FactoryName})", factoryNameString);

        // Mark execution mode in properties for resume capability after AwaitingClient
        context.Properties["_execution_mode_for_resume"] = "Planning";

        // Check if resuming with an existing plan (restored from cache after AwaitingClient)
        var existingPlan = context.ExecutionPlan;
        if (existingPlan != null && existingPlan.Steps.Any(s => !s.IsCompleted))
        {
            // Resume plan execution from where we left off
            await foreach (var response in ExecutePlanAsync(dependencies, sceneExecutor, finalResponseGenerator, context, settings, existingPlan, cancellationToken))
            {
                yield return response;
            }
            yield break;
        }

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
            // Direct answer available - no scenes to execute
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.Running,
                Message = plan.Reasoning,
                Contents = plan.Contents?.ToList()
            });
            context.ExecutionPhase = ExecutionPhase.Completed;
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
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Execute each step in order
        foreach (var step in plan.Steps.OrderBy(s => s.StepNumber))
        {
            if (step.IsCompleted)
            {
                continue;
            }

            // Check budget before executing next step
            if (settings.MaxBudget.HasValue && context.TotalCost > settings.MaxBudget.Value)
            {
                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.BudgetExceeded,
                    Message = $"Budget limit of {settings.MaxBudget:F6} exceeded during plan execution. Total cost: {context.TotalCost:F6}",
                    ErrorMessage = "Maximum budget reached during plan execution"
                });
                context.ExecutionPhase = ExecutionPhase.BudgetExceeded;
                yield break;
            }

            _logger.LogInformation("Planning step {Step}/{Total}: executing scene '{SceneName}'",
                step.StepNumber, plan.Steps.Count, step.SceneName);
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

                // If scene is awaiting client interaction or command execution,
                // stop the entire plan — the conversation has an unresolved tool_calls message.
                // Plan state (step.IsCompleted) is preserved via ExecutionPlan in cache,
                // so remaining steps will resume after client responds.
                if (response.Status == AiResponseStatus.AwaitingClient
                    || response.Status == AiResponseStatus.CommandClient)
                {
                    yield break;
                }
            }

            step.IsCompleted = true;
            _logger.LogDebug("Planning step {Step} ('{SceneName}') completed", step.StepNumber, step.SceneName);
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
