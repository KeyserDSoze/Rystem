using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework.Api.Infrastructure;

/// <summary>
/// Adapter for Azure OpenAI that implements IChatClient from Microsoft.Extensions.AI.
/// Supports tool calling, streaming, and comprehensive logging.
/// </summary>
public sealed class AzureOpenAIChatClientAdapter : IChatClient
{
    private readonly AzureOpenAIClient _azureClient;
    private readonly OpenAI.Chat.ChatClient _chatClient;
    private readonly string _deploymentName;
    private readonly ILogger<AzureOpenAIChatClientAdapter> _logger;

    public AzureOpenAIChatClientAdapter(
        string endpoint,
        string apiKey,
        string deploymentName,
        ILogger<AzureOpenAIChatClientAdapter>? logger = null)
    {
        _deploymentName = deploymentName;
        _azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _chatClient = _azureClient.GetChatClient(deploymentName);
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AzureOpenAIChatClientAdapter>.Instance;
    }

    public ChatClientMetadata Metadata => new(
        providerName: "AzureOpenAI",
        providerUri: new Uri("https://azure.microsoft.com/products/ai-services/openai-service"),
        _deploymentName);

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var openAiMessages = ConvertMessages(messages);
        var chatOptions = ConvertChatOptions(options, isStreaming: false);

        var response = await _chatClient.CompleteChatAsync(openAiMessages, chatOptions, cancellationToken);

        LogResponse(response.Value);

        return ConvertToAIResponse(response.Value);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var openAiMessages = ConvertMessages(messages);
        var chatOptions = ConvertChatOptions(options, isStreaming: true);

        await foreach (var streamUpdate in _chatClient.CompleteChatStreamingAsync(openAiMessages, chatOptions, cancellationToken))
        {
            // Handle text content
            foreach (var contentPart in streamUpdate.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentPart.Text))
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, contentPart.Text);
                }
            }

            // Handle tool calls (appear complete in Azure SDK)
            foreach (var toolCallUpdate in streamUpdate.ToolCallUpdates)
            {
                if (!string.IsNullOrEmpty(toolCallUpdate.ToolCallId) && !string.IsNullOrEmpty(toolCallUpdate.FunctionName))
                {
                    // Handle BinaryData safely - it can be null or empty for functions without arguments
                    var argsJson = string.Empty;
                    if (toolCallUpdate.FunctionArgumentsUpdate != null && !toolCallUpdate.FunctionArgumentsUpdate.ToMemory().IsEmpty)
                    {
                        argsJson = toolCallUpdate.FunctionArgumentsUpdate.ToString();
                    }

                    var argsDict = ParseFunctionArguments(argsJson);

                    _logger.LogInformation("[LLM STREAMING] Tool call: {ToolName} with args: {Args}",
                        toolCallUpdate.FunctionName, string.IsNullOrEmpty(argsJson) ? "{}" : argsJson);

                    var funcCall = new FunctionCallContent(
                        toolCallUpdate.ToolCallId,
                        toolCallUpdate.FunctionName,
                        argsDict);

                    yield return new ChatResponseUpdate(ChatRole.Assistant, [funcCall]);
                }
            }
        }
    }

    private List<OpenAI.Chat.ChatMessage> ConvertMessages(IEnumerable<ChatMessage> messages)
    {
        var openAiMessages = new List<OpenAI.Chat.ChatMessage>();

        foreach (var msg in messages)
        {
            if (msg.Contents is { Count: > 0 })
            {
                // Handle multi-part content (text + function calls + function results)
                foreach (var content in msg.Contents)
                {
                    switch (content)
                    {
                        case TextContent textContent:
                            openAiMessages.Add(CreateOpenAIMessage(msg.Role.Value, textContent.Text ?? string.Empty));
                            break;

                        case FunctionCallContent funcCall:
                            var toolCall = OpenAI.Chat.ChatToolCall.CreateFunctionToolCall(
                                funcCall.CallId ?? Guid.NewGuid().ToString(),
                                funcCall.Name,
                                BinaryData.FromString(funcCall.Arguments?.ToString() ?? "{}"));
                            openAiMessages.Add(OpenAI.Chat.ChatMessage.CreateAssistantMessage([toolCall]));
                            break;

                        case FunctionResultContent funcResult:
                            openAiMessages.Add(OpenAI.Chat.ChatMessage.CreateToolMessage(
                                funcResult.CallId ?? Guid.NewGuid().ToString(),
                                funcResult.Result?.ToString() ?? ""));
                            break;
                    }
                }
            }
            else
            {
                // Simple message with only text
                openAiMessages.Add(CreateOpenAIMessage(msg.Role.Value, msg.Text ?? string.Empty));
            }
        }

        return openAiMessages;
    }

    private OpenAI.Chat.ChatMessage CreateOpenAIMessage(string role, string text)
    {
        return role switch
        {
            "system" => OpenAI.Chat.ChatMessage.CreateSystemMessage(text),
            "user" => OpenAI.Chat.ChatMessage.CreateUserMessage(text),
            "assistant" => OpenAI.Chat.ChatMessage.CreateAssistantMessage(text),
            _ => OpenAI.Chat.ChatMessage.CreateUserMessage(text)
        };
    }

    private OpenAI.Chat.ChatCompletionOptions ConvertChatOptions(ChatOptions? options, bool isStreaming)
    {
        var chatOptions = new OpenAI.Chat.ChatCompletionOptions();

        if (options?.Tools is { Count: > 0 })
        {
            var logPrefix = isStreaming ? "[LLM STREAMING]" : "[LLM REQUEST]";
            _logger.LogInformation("{Prefix} Sending {ToolCount} tools to LLM", logPrefix, options.Tools.Count);

            foreach (var tool in options.Tools)
            {
                if (tool is AIFunction aiFunc)
                {
                    var functionDef = OpenAI.Chat.ChatTool.CreateFunctionTool(
                        functionName: aiFunc.Name,
                        functionDescription: aiFunc.Description);

                    chatOptions.Tools.Add(functionDef);
                    
                    if (!isStreaming)
                    {
                        _logger.LogInformation("  - Tool: {ToolName} | {ToolDescription}", aiFunc.Name, aiFunc.Description);
                    }
                    else
                    {
                        _logger.LogInformation("  - Tool: {ToolName}", aiFunc.Name);
                    }
                }
            }
        }
        else if (!isStreaming)
        {
            _logger.LogWarning("[LLM REQUEST] No tools provided to LLM - scene selection will not work!");
        }

        return chatOptions;
    }

    private void LogResponse(OpenAI.Chat.ChatCompletion completion)
    {
        _logger.LogInformation("[LLM RESPONSE] Finish reason: {FinishReason} | Tool calls: {ToolCallCount}",
            completion.FinishReason,
            completion.ToolCalls.Count);

        foreach (var toolCall in completion.ToolCalls)
        {
            if (toolCall is OpenAI.Chat.ChatToolCall chatToolCall)
            {
                _logger.LogInformation("  - LLM called tool: {ToolName} with args: {Args}",
                    chatToolCall.FunctionName,
                    chatToolCall.FunctionArguments.ToString());
            }
        }
    }

    private ChatResponse ConvertToAIResponse(OpenAI.Chat.ChatCompletion completion)
    {
        var chatMessage = new ChatMessage(
            ChatRole.Assistant,
            completion.Content.FirstOrDefault()?.Text ?? string.Empty);

        // Add tool calls if present
        if (completion.ToolCalls.Count > 0)
        {
            foreach (var toolCall in completion.ToolCalls)
            {
                if (toolCall is OpenAI.Chat.ChatToolCall chatToolCall)
                {
                    var argsDict = ParseFunctionArguments(chatToolCall.FunctionArguments.ToString());

                    chatMessage.Contents.Add(new FunctionCallContent(
                        chatToolCall.Id,
                        chatToolCall.FunctionName,
                        argsDict));
                }
            }
        }

        return new ChatResponse([chatMessage])
        {
            ModelId = _deploymentName,
            Usage = new UsageDetails
            {
                InputTokenCount = completion.Usage.InputTokenCount,
                OutputTokenCount = completion.Usage.OutputTokenCount,
                TotalTokenCount = completion.Usage.TotalTokenCount
            }
        };
    }

    private static Dictionary<string, object?> ParseFunctionArguments(string? argsJson)
    {
        var argsDict = new Dictionary<string, object?>();

        if (string.IsNullOrEmpty(argsJson))
            return argsDict;

        try
        {
            var parsedArgs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(argsJson);

            if (parsedArgs != null)
            {
                foreach (var kvp in parsedArgs)
                {
                    argsDict[kvp.Key] = kvp.Value.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.String => kvp.Value.GetString(),
                        System.Text.Json.JsonValueKind.Number => kvp.Value.GetDouble(),
                        System.Text.Json.JsonValueKind.True => true,
                        System.Text.Json.JsonValueKind.False => false,
                        System.Text.Json.JsonValueKind.Null => null,
                        _ => kvp.Value.ToString()
                    };
                }
            }
        }
        catch
        {
            // Fallback: use raw string
            argsDict["raw"] = argsJson;
        }

        return argsDict;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
