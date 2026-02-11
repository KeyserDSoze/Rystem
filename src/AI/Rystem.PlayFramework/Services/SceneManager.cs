using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Main orchestrator for PlayFramework execution.
/// </summary>
internal sealed class SceneManager : ISceneManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISceneFactory _sceneFactory;
    private readonly IChatClient _chatClientFactory;
    private readonly PlayFrameworkSettings _settings;
    private readonly List<ActorConfiguration> _mainActors;
    private readonly IPlanner? _planner;
    private readonly ISummarizer? _summarizer;
    private readonly IDirector? _director;
    private readonly ICacheService? _cacheService;

    public SceneManager(
        IServiceProvider serviceProvider,
        ISceneFactory sceneFactory,
        IChatClient chatClientFactory,
        PlayFrameworkSettings settings,
        List<ActorConfiguration> mainActors,
        IPlanner? planner = null,
        ISummarizer? summarizer = null,
        IDirector? director = null,
        ICacheService? cacheService = null)
    {
        _serviceProvider = serviceProvider;
        _sceneFactory = sceneFactory;
        _chatClientFactory = chatClientFactory;
        _settings = settings;
        _mainActors = mainActors;
        _planner = planner;
        _summarizer = summarizer;
        _director = director;
        _cacheService = cacheService;
    }

    public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
        string message,
        SceneRequestSettings? settings = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        settings ??= new SceneRequestSettings();
        
        // Initialize
        yield return YieldStatus(AiResponseStatus.Initializing, "Initializing context");

        var context = await InitializeContextAsync(message, settings, cancellationToken);

        // Load from cache if available
        if (settings.CacheBehavior != CacheBehavior.Avoidable && _cacheService != null && context.CacheKey != null)
        {
            yield return YieldStatus(AiResponseStatus.LoadingCache, "Loading from cache");
            
            var cached = await _cacheService.GetAsync(context.CacheKey, cancellationToken);
            if (cached != null)
            {
                // Check if summarization needed for cached data
                if (_summarizer != null && _summarizer.ShouldSummarize(cached))
                {
                    yield return YieldStatus(AiResponseStatus.Summarizing, "Summarizing cached conversation");
                    
                    var summary = await _summarizer.SummarizeAsync(cached, cancellationToken);
                    context.ConversationSummary = summary;
                    
                    // Add summary to chat
                    // Store summary in context to include in future messages
                    context.Properties["conversation_summary"] = summary;
                }
                else
                {
                    // Replay cached messages
                    foreach (var cachedResponse in cached)
                    {
                        context.Responses.Add(cachedResponse);
                    }
                }
            }
        }

        // Execute main actors
        yield return YieldStatus(AiResponseStatus.ExecutingMainActors, "Executing main actors");
        await ExecuteMainActorsAsync(context, cancellationToken);

        // Planning mode or direct execution
        if (settings.EnablePlanning && _planner != null)
        {
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
        else
        {
            // Direct execution (no planning)
            await foreach (var response in RequestAsync(context, settings, cancellationToken))
            {
                yield return response;
            }
        }

        // Save to cache
        if (settings.CacheBehavior != CacheBehavior.Avoidable && _cacheService != null && context.CacheKey != null)
        {
            yield return YieldStatus(AiResponseStatus.SavingCache, "Saving to cache");
            await _cacheService.SetAsync(context.CacheKey, context.Responses, settings.CacheBehavior, cancellationToken);
        }

        yield return YieldStatus(AiResponseStatus.Completed, "Execution completed", context.TotalCost);
    }

    private async Task<SceneContext> InitializeContextAsync(
        string message,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        // Create chat client
        var chatClient = _chatClientFactory;

        // Apply settings
        if (!string.IsNullOrEmpty(settings.ModelId))
        {
            // Model override would be applied here
        }

        var context = new SceneContext
        {
            ServiceProvider = _serviceProvider,
            InputMessage = message,
            ChatClient = chatClient,
            CacheKey = settings.CacheKey ?? Guid.NewGuid().ToString(),
            CacheBehavior = settings.CacheBehavior
        };

        return context;
    }

    private async Task ExecuteMainActorsAsync(SceneContext context, CancellationToken cancellationToken)
    {
        foreach (var actorConfig in _mainActors)
        {
            var actor = ActorFactory.Create(actorConfig, _serviceProvider);
            var response = await actor.PlayAsync(context, cancellationToken);

            if (!string.IsNullOrWhiteSpace(response.Message))
            {
                // Store actor message in context
                context.Properties[$"main_actor_{_mainActors.IndexOf(actorConfig)}"] = response.Message;

                // Track for planner
                context.MainActorContext.Add(response.Message);
            }
        }
    }

    private async IAsyncEnumerable<AiSceneResponse> ExecutePlanAsync(
        SceneContext context,
        SceneRequestSettings settings,
        ExecutionPlan plan,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken,
        int recursionDepth = 0)
    {
        if (recursionDepth >= settings.MaxRecursionDepth)
        {
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.Error,
                ErrorMessage = $"Maximum recursion depth ({settings.MaxRecursionDepth}) reached"
            });
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
            var scene = FindSceneByFuzzyMatch(step.SceneName);
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
            await foreach (var response in ExecuteSceneAsync(context, scene, cancellationToken))
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
            await foreach (var response in GenerateFinalResponseAsync(context, cancellationToken))
            {
                yield return response;
            }
        }
    }

    private async IAsyncEnumerable<AiSceneResponse> RequestAsync(
        SceneContext context,
        SceneRequestSettings settings,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Add user message
        var userMessage = new ChatMessage(ChatRole.User, context.InputMessage);

        // Get all scenes as tools for selection
        var sceneTools = _sceneFactory.GetSceneNames()
            .Select(name => _sceneFactory.Create(name))
            .Select(scene => CreateSceneSelectionTool(scene))
            .ToList();

        // Configure chat with scene selection tools
        var chatOptions = new ChatOptions
        {
            Tools = sceneTools.Cast<AITool>().ToList()
        };

        // Call LLM for scene selection
        var response = await context.ChatClient.GetResponseAsync(
            new[] { userMessage },
            chatOptions,
            cancellationToken);

        // Process function calls (scene selections)
        var responseMessage = response.Messages?.FirstOrDefault();
        if (responseMessage?.Contents != null)
        {
            foreach (var content in responseMessage.Contents)
            {
                if (content is FunctionCallContent functionCall)
                {
                    // Scene selection via function call
                    var selectedSceneName = functionCall.Name;

                    yield return YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.Running,
                        Message = $"Selected scene: {selectedSceneName}",
                        SceneName = selectedSceneName
                    });

                    // Execute the selected scene
                    var scene = FindSceneByFuzzyMatch(selectedSceneName);
                    if (scene != null)
                    {
                        await foreach (var sceneResponse in ExecuteSceneAsync(context, scene, cancellationToken))
                        {
                            yield return sceneResponse;
                        }
                    }
                    else
                    {
                        yield return YieldAndTrack(context, new AiSceneResponse
                        {
                            Status = AiResponseStatus.Error,
                            Message = $"Scene '{selectedSceneName}' not found"
                        });
                    }

                    yield break; // Exit after processing scene selection
                }
            }
        }

        // Fallback: if no function call, return text response
        yield return YieldAndTrack(context, new AiSceneResponse
        {
            Status = AiResponseStatus.Running,
            Message = responseMessage?.Text ?? "No response from LLM"
        });
    }

    private async IAsyncEnumerable<AiSceneResponse> ExecuteSceneAsync(
        SceneContext context,
        IScene scene,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return YieldStatus(AiResponseStatus.ExecutingScene, $"Entering scene: {scene.Name}");

        // Execute scene actors
        await scene.ExecuteActorsAsync(context, cancellationToken);

        // Get scene tools
        var sceneTools = scene.GetTools().ToList();
        var sceneToolsFunctions = sceneTools.Select(t => t.ToAIFunction()).ToList();

        // Build conversation messages
        var conversationMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, context.InputMessage)
        };

        // Add main actor context if available
        if (context.MainActorContext.Count > 0)
        {
            var mainActorText = string.Join("\n\n", context.MainActorContext);
            conversationMessages.Add(new ChatMessage(ChatRole.System, mainActorText));
        }

        var chatOptions = new ChatOptions
        {
            Tools = sceneToolsFunctions.Cast<AITool>().ToList()
        };

        // Tool calling loop - continue until LLM stops calling tools
        const int MaxToolCallIterations = 10;
        var iteration = 0;

        while (iteration < MaxToolCallIterations)
        {
            iteration++;

            // Call LLM
            var response = await context.ChatClient.GetResponseAsync(
                conversationMessages,
                chatOptions,
                cancellationToken);

            var responseMessage = response.Messages?.FirstOrDefault();
            if (responseMessage == null)
            {
                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.Error,
                    SceneName = scene.Name,
                    Message = "No response from LLM"
                });
                yield break;
            }

            // Add assistant message to conversation
            conversationMessages.Add(responseMessage);

            // Check for function calls
            var functionCalls = responseMessage.Contents?.
                OfType<FunctionCallContent>()
                .ToList() ?? [];

            if (functionCalls.Count == 0)
            {
                // No more function calls - return final text response
                yield return YieldAndTrack(context, new AiSceneResponse
                {
                    Status = AiResponseStatus.Running,
                    SceneName = scene.Name,
                    Message = responseMessage.Text ?? string.Empty
                });
                yield break;
            }

            // Execute each function call
            foreach (var functionCall in functionCalls)
            {
                // Find the tool
                var tool = sceneTools.FirstOrDefault(t => t.Name == functionCall.Name);
                if (tool == null)
                {
                    // Tool not found - send error result
                    var errorResult = new FunctionResultContent(functionCall.CallId, functionCall.Name) 
                    { 
                        Result = $"Tool '{functionCall.Name}' not found" 
                    };
                    conversationMessages.Add(new ChatMessage(ChatRole.Tool, [errorResult]));
                    continue;
                }

                // Track tool execution
                var toolKey = $"{scene.Name}.{functionCall.Name}";
                context.ExecutedTools.Add(toolKey);

                // Execute tool - store responses before yielding
                var statusResponse = YieldStatus(AiResponseStatus.FunctionRequest, $"Executing tool: {functionCall.Name}");
                yield return statusResponse;

                AiSceneResponse resultResponse;
                try
                {
                    // Serialize arguments to JSON
                    var argsJson = System.Text.Json.JsonSerializer.Serialize(functionCall.Arguments ?? new Dictionary<string, object?>());

                    // Execute the tool
                    var toolResult = await tool.ExecuteAsync(argsJson, context, cancellationToken);

                    // Send result back to LLM
                    var functionResult = new FunctionResultContent(functionCall.CallId, functionCall.Name) 
                    { 
                        Result = toolResult 
                    };
                    conversationMessages.Add(new ChatMessage(ChatRole.Tool, [functionResult]));

                    resultResponse = YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.FunctionCompleted,
                        SceneName = scene.Name,
                        Message = $"Tool {functionCall.Name} executed: {toolResult}",
                        FunctionName = functionCall.Name
                    });
                }
                catch (Exception ex)
                {
                    // Send error result
                    var errorResult = new FunctionResultContent(functionCall.CallId, functionCall.Name) 
                    { 
                        Result = $"Error executing tool: {ex.Message}" 
                    };
                    conversationMessages.Add(new ChatMessage(ChatRole.Tool, [errorResult]));

                    resultResponse = YieldAndTrack(context, new AiSceneResponse
                    {
                        Status = AiResponseStatus.Error,
                        SceneName = scene.Name,
                        ErrorMessage = ex.Message,
                        Message = $"Tool execution failed: {ex.Message}",
                        FunctionName = functionCall.Name
                    });
                }

                yield return resultResponse;
            }

            // Continue loop to get next LLM response after function results
        }

        // Max iterations reached
        yield return YieldAndTrack(context, new AiSceneResponse
        {
            Status = AiResponseStatus.Error,
            SceneName = scene.Name,
            Message = $"Maximum tool call iterations ({MaxToolCallIterations}) reached"
        });
    }

    private async IAsyncEnumerable<AiSceneResponse> GenerateFinalResponseAsync(
        SceneContext context,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return YieldStatus(AiResponseStatus.GeneratingFinalResponse, "Generating final response");

        // Check if any scene already provided a SPECIFIC_COMMAND
        var directAnswer = context.Responses
            .Where(r => r.Status == AiResponseStatus.Running && 
                       r.Message?.Contains("SPECIFIC_COMMAND:") == true)
            .LastOrDefault();

        if (directAnswer != null)
        {
            yield return YieldAndTrack(context, new AiSceneResponse
            {
                Status = AiResponseStatus.Running,
                Message = directAnswer.Message
            });
            yield break;
        }

        // Generate final response based on gathered data
        var finalPrompt = new ChatMessage(ChatRole.User, "Based on all the information gathered, provide the final answer to the user's request.");
        
        var response = await context.ChatClient.GetResponseAsync(
            new[] { finalPrompt },
            cancellationToken: cancellationToken);

        yield return YieldAndTrack(context, new AiSceneResponse
        {
            Status = AiResponseStatus.Running,
            Message = response.Messages?.FirstOrDefault()?.Text
        });
    }

    private IScene? FindSceneByFuzzyMatch(string requestedName)
    {
        // Normalize scene names for matching
        var normalized = NormalizeSceneName(requestedName);

        foreach (var sceneName in _sceneFactory.GetSceneNames())
        {
            if (NormalizeSceneName(sceneName) == normalized)
            {
                return _sceneFactory.Create(sceneName);
            }
        }

        return null;
    }

    private static string NormalizeSceneName(string name)
    {
        return name.Replace("-", "")
                  .Replace("_", "")
                  .Replace(" ", "")
                  .ToLowerInvariant()
                  .Trim();
    }

    private static AIFunction CreateSceneSelectionTool(IScene scene)
    {
        return AIFunctionFactory.Create(
            (string input) => scene.Name,
            scene.Name,
            scene.Description);
    }

    private AiSceneResponse YieldStatus(AiResponseStatus status, string? message = null, decimal? cost = null)
    {
        return new AiSceneResponse
        {
            Status = status,
            Message = message,
            Cost = cost,
            TotalCost = cost ?? 0
        };
    }

    private AiSceneResponse YieldAndTrack(SceneContext context, AiSceneResponse response)
    {
        response.TotalCost = context.TotalCost;
        context.Responses.Add(response);
        return response;
    }
}
