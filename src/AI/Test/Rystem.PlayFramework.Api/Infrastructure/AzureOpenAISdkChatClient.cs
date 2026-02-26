using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using static System.Net.Mime.MediaTypeNames;

namespace Rystem.PlayFramework.Api.Infrastructure;

/// <summary>
/// Minimal IChatClient implementation using Azure OpenAI SDK directly.
/// This replaces the older adapter in DI while keeping that adapter in the repo for reference.
/// It ensures function "parameters" (JSON schema) are included when sending tools to the model.
/// </summary>
public sealed class AzureOpenAISdkChatClient : IChatClient
{
    private readonly AzureOpenAIClient _azureClient;
    private readonly OpenAI.Chat.ChatClient _chatClient;
    private readonly string _deploymentName;
    private readonly ILogger<AzureOpenAISdkChatClient> _logger;

    public AzureOpenAISdkChatClient(string endpoint, string apiKey, string deploymentName, ILogger<AzureOpenAISdkChatClient>? logger = null)
    {
        _deploymentName = deploymentName;
        _azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _chatClient = _azureClient.GetChatClient(deploymentName);
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AzureOpenAISdkChatClient>.Instance;
    }

    public ChatClientMetadata Metadata => new(
        providerName: "AzureOpenAI",
        providerUri: new Uri("https://azure.microsoft.com/products/ai-services/openai-service"),
        _deploymentName);

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var openAiMessages = ConvertMessages(messages);
        var chatOptions = ConvertChatOptions(options, isStreaming: false);

        var response = await _chatClient.CompleteChatAsync(openAiMessages, chatOptions, cancellationToken);

        LogResponse(response.Value);

        return ConvertToAIResponse(response.Value);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var openAiMessages = ConvertMessages(messages);
        var chatOptions = ConvertChatOptions(options, isStreaming: true);

        await foreach (var streamUpdate in _chatClient.CompleteChatStreamingAsync(openAiMessages, chatOptions, cancellationToken))
        {
            foreach (var contentPart in streamUpdate.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentPart.Text))
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, contentPart.Text);
                }
            }

            foreach (var toolCallUpdate in streamUpdate.ToolCallUpdates)
            {
                if (!string.IsNullOrEmpty(toolCallUpdate.ToolCallId) && !string.IsNullOrEmpty(toolCallUpdate.FunctionName))
                {
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

                    yield return new ChatResponseUpdate(ChatRole.Assistant, new[] { funcCall });
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
                                BinaryData.FromObjectAsJson(funcCall.Arguments));
                            openAiMessages.Add(OpenAI.Chat.ChatMessage.CreateAssistantMessage(new[] { toolCall }));
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
            foreach (var tool in options.Tools)
            {
                var name = tool.Name;
                var description = tool.Description;
                string jsonSchemaAsText = "{}";
                if (tool is AIFunction aiFunc)
                {
                    // Generate JSON schema using AIJsonUtilities
                    jsonSchemaAsText = aiFunc.JsonSchema.ToString();
                }
                else if (tool is AIFunctionDeclaration aIFunctionDeclaration)
                {
                    jsonSchemaAsText = aIFunctionDeclaration.JsonSchema.ToString();
                }
                else
                {
                    var errorMsg = $"Unsupported tool type: {tool.GetType().FullName}. Only AIFunction and AIFunctionDeclaration are supported.";
                    _logger.LogError(errorMsg);
                }
                var parametersSchema = BinaryData.FromString(jsonSchemaAsText.Replace("\"type\":[\"object\",\"null\"]", "\"type\":\"object\""));
                // Create tool with or without schema
                var functionDef = parametersSchema != null
                    ? OpenAI.Chat.ChatTool.CreateFunctionTool(
                        functionName: name,
                        functionDescription: description,
                        functionParameters: parametersSchema)
                    : OpenAI.Chat.ChatTool.CreateFunctionTool(
                        functionName: name,
                        functionDescription: description);
                chatOptions.Tools.Add(functionDef);
            }
        }

        return chatOptions;
    }

    private void LogResponse(OpenAI.Chat.ChatCompletion completion)
    {
        // Intentionally minimal: do not emit logs here to keep client behavior simple.
    }

    private ChatResponse ConvertToAIResponse(OpenAI.Chat.ChatCompletion completion)
    {
        var chatMessage = new ChatMessage(
            ChatRole.Assistant,
            completion.Content.FirstOrDefault()?.Text ?? string.Empty);

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

        return new ChatResponse(new[] { chatMessage })
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
            argsDict["raw"] = argsJson;
        }

        return argsDict;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
