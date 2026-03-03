using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework;

/// <summary>
/// Deterministic planner that uses LLM to create structured execution plans.
/// Uses ExecutionPlan directly as the tool schema — no intermediate DTOs.
/// </summary>
internal sealed class DeterministicPlanner : IPlanner
{
    private readonly IChatClientManager _chatClientManager;
    private readonly ISceneFactory _sceneFactory;
    private readonly PlayFrameworkSettings _settings;
    private readonly ILogger<DeterministicPlanner> _logger;

    /// <summary>
    /// Static tool definition: LLM fills an ExecutionPlan via forced function calling.
    /// </summary>
    private static readonly AIFunction s_planningTool = AIFunctionFactory.Create(
        (ExecutionPlan plan) => plan,
        name: "create_execution_plan",
        description: "Creates a structured execution plan based on the user request and available scenes. Set needsExecution=true and populate steps when scenes need to run. Set needsExecution=false with reasoning when the answer is already available.",
        JsonHelper.JsonSerializerOptions);

    public DeterministicPlanner(
        IChatClientManager chatClientManager,
        ISceneFactory sceneFactory,
        PlayFrameworkSettings settings,
        ILogger<DeterministicPlanner> logger)
    {
        _chatClientManager = chatClientManager;
        _sceneFactory = sceneFactory;
        _settings = settings;
        _logger = logger;
    }

    public async Task<ExecutionPlan> CreatePlanAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, BuildSystemPrompt()),
            new(ChatRole.User, BuildUserPrompt(context))
        };

        var options = new ChatOptions
        {
            Tools = [s_planningTool],
            ToolMode = ChatToolMode.RequireAny,
            Temperature = 0.1f
        };

        var responseWithCost = await _chatClientManager.GetResponseAsync(messages, options, cancellationToken);

        var planningMessage = responseWithCost.Response.Messages?.FirstOrDefault();
        var planningContents = planningMessage?.Contents?
            .Where(c => c is DataContent or UriContent)
            .ToList();

        // Extract the tool call
        var toolCall = planningMessage?.Contents?.OfType<FunctionCallContent>().FirstOrDefault();
        if (toolCall?.Arguments == null)
        {
            _logger.LogWarning("DeterministicPlanner: LLM did not return a tool call");
            return new ExecutionPlan
            {
                NeedsExecution = false,
                Reasoning = "Failed to get execution plan from LLM"
            };
        }

        // Parse: AIFunctionFactory wraps under parameter name "plan",
        // so arguments = { "plan": { needsExecution, reasoning, steps, ... } }
        ExecutionPlan? plan = null;
        try
        {
            // Unwrap the "plan" key if present (single-parameter wrapper)
            object? target = toolCall.Arguments;
            if (toolCall.Arguments is { Count: 1 })
            {
                target = toolCall.Arguments.Values.First() ?? target;
            }

            // The value may already be a JSON string (not an object to re-serialize).
            // If so, use it directly; otherwise serialize to get JSON.
            string json;
            if (target is string strValue)
            {
                json = strValue;
            }
            else if (target is JsonElement je && je.ValueKind == JsonValueKind.String)
            {
                json = je.GetString()!;
            }
            else
            {
                json = JsonSerializer.Serialize(target, JsonHelper.JsonSerializerOptions);
            }

            _logger.LogDebug("DeterministicPlanner toolCall JSON: {Json}", json);

            plan = JsonSerializer.Deserialize<ExecutionPlan>(json, JsonHelper.JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeterministicPlanner failed to deserialize ExecutionPlan");
        }

        if (plan == null || plan.Reasoning == null)
        {
            _logger.LogWarning("DeterministicPlanner: deserialization returned null or empty plan");
            return new ExecutionPlan
            {
                NeedsExecution = false,
                Reasoning = "Failed to parse execution plan"
            };
        }

        // Attach multi-modal contents from the planning response
        plan.Contents = planningContents;
        plan.CreatedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "DeterministicPlanner created plan - NeedsExecution: {NeedsExec}, Steps: {StepCount}, Reasoning: {Reasoning}",
            plan.NeedsExecution, plan.Steps.Count, plan.Reasoning);

        return plan;
    }

    /// <summary>
    /// System prompt that explains what the planner must do and lists all available scenes with their tools.
    /// </summary>
    private string BuildSystemPrompt()
    {
        var scenesDescription = string.Join("\n", _sceneFactory.Scenes.Select(scene =>
        {
            var toolList = scene.Tools.Count > 0
                ? string.Join(", ", scene.Tools.Select(t => $"{t.Name} ({t.Description})"))
                : "no tools";
            return $"- Scene \"{scene.Name}\": {scene.Description ?? "no description"}\n  Tools: {toolList}";
        }));

        return $@"You are an execution planner. Analyze the user request and determine which scenes need to execute.

AVAILABLE SCENES:
{scenesDescription}

RULES:
1. Check conversation history FIRST — data may already be available
2. If the answer is already in context, set needsExecution=false with reasoning
3. If execution is needed, create a step-by-step plan using EXACT scene names from the list above
4. Each step must reference a valid scene name and list the expected tools to call
5. Steps are executed in order; use dependsOnStep to indicate data dependencies
6. Keep plans minimal — only include necessary steps

USE THE create_execution_plan TOOL to return your structured plan.";
    }

    /// <summary>
    /// User prompt with the actual request and conversation history.
    /// </summary>
    private static string BuildUserPrompt(SceneContext context)
    {
        var promptData = new
        {
            userRequest = context.InputMessage,
            conversationHistory = context.ConversationHistory
                .Where(m => m.Label != "InitialContext" && m.Label != "MemoryContext")
                .Select(m => new { role = m.Message.Role.Value, label = m.Label, text = m.Message.Text })
        };

        return JsonSerializer.Serialize(promptData, JsonHelper.JsonSerializerOptions);
    }
}
