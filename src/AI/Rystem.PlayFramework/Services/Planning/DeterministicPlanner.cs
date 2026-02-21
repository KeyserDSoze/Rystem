using System.Text.Json;
using Microsoft.Extensions.AI;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework;

/// <summary>
/// Deterministic planner that uses LLM to create structured execution plans.
/// </summary>
internal sealed class DeterministicPlanner : IPlanner
{
    private readonly IChatClientManager _chatClientManager;
    private readonly ISceneFactory _sceneFactory;
    private readonly PlayFrameworkSettings _settings;

    public DeterministicPlanner(
        IChatClientManager chatClientManager,
        ISceneFactory sceneFactory,
        PlayFrameworkSettings settings)
    {
        _chatClientManager = chatClientManager;
        _sceneFactory = sceneFactory;
        _settings = settings;
    }

    public async Task<ExecutionPlan> CreatePlanAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken = default)
    {
        // Build planning prompt
        var planningPrompt = BuildPlanningPrompt(context);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, GetPlanningSystemPrompt()),
            new(ChatRole.User, planningPrompt)
        };

        // Create forced tool for ExecutionPlanDto
        var planningTool = AIFunctionFactory.Create(
            (ExecutionPlanDto plan) => plan,
            name: "create_execution_plan",
            description: "Creates a structured execution plan based on the user request and available scenes.",
            JsonHelper.JsonSerializerOptions);

        // Get plan from LLM using forced tool
        var options = new ChatOptions
        {
            Tools = [planningTool],
            ToolMode = ChatToolMode.RequireAny,
            Temperature = 0.1f // Low temperature for consistent planning
        };

        var responseWithCost = await _chatClientManager.GetResponseAsync(messages, options, cancellationToken);

        // Extract multi-modal contents from planning response
        var planningMessage = responseWithCost.Response.Messages?.FirstOrDefault();
        var planningContents = planningMessage?.Contents?
            .Where(c => c is DataContent or UriContent)
            .ToList();

        // Extract plan from tool call
        var toolCall = planningMessage?.Contents?.OfType<FunctionCallContent>().FirstOrDefault();
        if (toolCall == null)
        {
            return new ExecutionPlan
            {
                NeedsExecution = false,
                Reasoning = "Failed to get execution plan from LLM"
            };
        }

        ExecutionPlanDto? plan = null;
        try
        {
            plan = new ExecutionPlanDto(
                (bool)toolCall.Arguments["needs_execution"],
                toolCall.Arguments["reasoning"].ToString(),
                JsonSerializer.Deserialize<List<PlanStepDto>>(toolCall.Arguments["steps"].ToString(), JsonHelper.JsonSerializerOptions));
        }
        catch (Exception ex)
        {
            var q = ex.ToString();
        }

        if (plan == null)
        {
            return new ExecutionPlan
            {
                NeedsExecution = false,
                Reasoning = "Failed to parse execution plan"
            };
        }

        // Convert to domain model
        return new ExecutionPlan
        {
            NeedsExecution = plan.NeedsExecution,
            Reasoning = plan.Reasoning,
            Contents = planningContents,
            Steps = plan.Steps?.Select(s => new PlanStep
            {
                StepNumber = s.StepNumber,
                SceneName = s.SceneName,
                Purpose = s.Purpose,
                ExpectedTools = s.ExpectedTools ?? [],
                DependsOnStep = s.DependsOnStep
            }).ToList() ?? []
        };
    }

    private string BuildPlanningPrompt(SceneContext context)
    {
        var availableScenes = _sceneFactory.Scenes
            .Select(scene => new
            {
                name = scene.Name,
                description = scene.Description,
                available_tools = scene.Tools.Select(t => new
                {
                    name = t.Name,
                    description = t.Description
                })
            });

        var promptData = new
        {
            user_request = context.InputMessage,
            conversation_history = context.ConversationHistory.Select(m => new { role = m.Message.Role.Value, label = m.Label }),
            available_scenes = availableScenes
        };

        return JsonSerializer.Serialize(promptData, JsonHelper.JsonSerializerOptions);
    }

    private static string GetPlanningSystemPrompt()
    {
        return @"You are an execution planner. Analyze the user request and conversation history to determine if execution is needed.

RULES:
1. Check conversation_history FIRST - data may already be available
2. If data is in context, set needs_execution=false with reasoning
3. If execution needed, create a logical step-by-step plan
4. Use EXACT scene names from available_scenes
5. Specify which tools should be called in each step
6. Indicate dependencies between steps

USE THE create_execution_plan TOOL to return your structured plan.";
    }

    // DTO for tool-based structured output
    private sealed record ExecutionPlanDto(
        bool NeedsExecution,
        string? Reasoning,
        List<PlanStepDto>? Steps
    );

    private sealed record PlanStepDto(
        int StepNumber,
        string SceneName,
        string? Purpose,
        List<string>? ExpectedTools,
        int? DependsOnStep
    );
}
