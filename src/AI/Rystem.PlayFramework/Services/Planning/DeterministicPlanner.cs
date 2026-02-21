using System.Text.Json;
using Microsoft.Extensions.AI;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework;

/// <summary>
/// Deterministic planner that uses LLM to create structured execution plans.
/// </summary>
internal sealed class DeterministicPlanner : IPlanner
{
    private readonly IChatClient _chatClient;
    private readonly ISceneFactory _sceneFactory;
    private readonly PlayFrameworkSettings _settings;

    public DeterministicPlanner(
        IChatClient chatClient,
        ISceneFactory sceneFactory,
        PlayFrameworkSettings settings)
    {
        _chatClient = chatClient;
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

        // Get plan from LLM
        var options = new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.Json,
            Temperature = 0.1f // Low temperature for consistent planning
        };

        var response = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
        var planJson = response.Messages?.FirstOrDefault()?.Text ?? "{}";

        // Extract multi-modal contents from planning response
        var planningMessage = response.Messages?.FirstOrDefault();
        var planningContents = planningMessage?.Contents?
            .Where(c => c is DataContent or UriContent)
            .ToList();

        // Parse plan
        var plan = JsonSerializer.Deserialize<ExecutionPlanDto>(planJson, JsonHelper.JsonSerializerOptions);

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
2. If data is in context, return needs_execution=false with reasoning
3. If execution needed, create a logical step-by-step plan
4. Use EXACT scene names from available_scenes
5. Specify which tools should be called in each step
6. Indicate dependencies between steps

OUTPUT FORMAT (JSON):
{
  ""needs_execution"": true/false,
  ""reasoning"": ""explanation"",
  ""steps"": [
    {
      ""step_number"": 1,
      ""scene_name"": ""exact scene name"",
      ""purpose"": ""what this step accomplishes"",
      ""expected_tools"": [""tool1"", ""tool2""],
      ""depends_on_step"": null or step number
    }
  ]
}";
    }

    // DTO for JSON deserialization
    private sealed class ExecutionPlanDto
    {
        public bool NeedsExecution { get; set; }
        public string? Reasoning { get; set; }
        public List<PlanStepDto>? Steps { get; set; }
    }

    private sealed class PlanStepDto
    {
        public int StepNumber { get; set; }
        public string SceneName { get; set; } = string.Empty;
        public string? Purpose { get; set; }
        public List<string>? ExpectedTools { get; set; }
        public int? DependsOnStep { get; set; }
    }
}
