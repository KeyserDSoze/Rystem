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

        // OpenAI streaming sends tool calls incrementally across multiple chunks:
        //   - First chunk for a tool: has ToolCallId + FunctionName (but partial/empty args)
        //   - Subsequent chunks: only have FunctionArgumentsUpdate (no Id/Name)
        // We accumulate by Index and emit complete FunctionCallContent only once
        // all argument fragments have been received.
        var toolCallAccumulators = new Dictionary<int, (string ToolCallId, string FunctionName, System.Text.StringBuilder Args)>();

        await foreach (var streamUpdate in _chatClient.CompleteChatStreamingAsync(openAiMessages, chatOptions, cancellationToken))
        {
            // Stream text content immediately
            foreach (var contentPart in streamUpdate.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentPart.Text))
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, contentPart.Text);
                }
            }

            // Accumulate tool call chunks by index
            foreach (var toolCallUpdate in streamUpdate.ToolCallUpdates)
            {
                var index = toolCallUpdate.Index;

                if (!toolCallAccumulators.TryGetValue(index, out var accumulator))
                {
                    accumulator = (
                        toolCallUpdate.ToolCallId ?? string.Empty,
                        toolCallUpdate.FunctionName ?? string.Empty,
                        new System.Text.StringBuilder()
                    );
                    toolCallAccumulators[index] = accumulator;
                }

                // Append argument fragment
                if (toolCallUpdate.FunctionArgumentsUpdate != null && !toolCallUpdate.FunctionArgumentsUpdate.ToMemory().IsEmpty)
                {
                    accumulator.Args.Append(toolCallUpdate.FunctionArgumentsUpdate.ToString());
                    toolCallAccumulators[index] = accumulator; // re-assign since value-tuple is a struct
                }
            }

            // When FinishReason arrives, emit all accumulated tool calls then the finish signal
            if (streamUpdate.FinishReason != null)
            {
                foreach (var kvp in toolCallAccumulators.OrderBy(x => x.Key))
                {
                    var (toolCallId, functionName, argsBuilder) = kvp.Value;
                    var argsJson = argsBuilder.ToString();
                    var argsDict = ParseFunctionArguments(argsJson);

                    _logger.LogInformation("[LLM STREAMING] Tool call: {ToolName} with args: {Args}",
                        functionName, string.IsNullOrEmpty(argsJson) ? "{}" : argsJson);

                    var funcCall = new FunctionCallContent(toolCallId, functionName, argsDict);
                    yield return new ChatResponseUpdate(ChatRole.Assistant, new[] { funcCall });
                }
                toolCallAccumulators.Clear();

                var finishReason = streamUpdate.FinishReason switch
                {
                    var fr when fr == OpenAI.Chat.ChatFinishReason.Stop => ChatFinishReason.Stop,
                    var fr when fr == OpenAI.Chat.ChatFinishReason.ToolCalls => ChatFinishReason.ToolCalls,
                    var fr when fr == OpenAI.Chat.ChatFinishReason.ContentFilter => ChatFinishReason.ContentFilter,
                    var fr when fr == OpenAI.Chat.ChatFinishReason.Length => ChatFinishReason.Length,
                    _ => ChatFinishReason.Stop
                };

                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    FinishReason = finishReason
                };
            }
        }

        // Fallback: if stream ended without a FinishReason chunk, emit any remaining tool calls
        if (toolCallAccumulators.Count > 0)
        {
            foreach (var kvp in toolCallAccumulators.OrderBy(x => x.Key))
            {
                var (toolCallId, functionName, argsBuilder) = kvp.Value;
                var argsJson = argsBuilder.ToString();
                var argsDict = ParseFunctionArguments(argsJson);

                _logger.LogInformation("[LLM STREAMING] Tool call (fallback): {ToolName} with args: {Args}",
                    functionName, string.IsNullOrEmpty(argsJson) ? "{}" : argsJson);

                var funcCall = new FunctionCallContent(toolCallId, functionName, argsDict);
                yield return new ChatResponseUpdate(ChatRole.Assistant, new[] { funcCall });
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
                // Collect all content parts and tool calls from a single ChatMessage.
                // A user message may contain both TextContent and DataContent (e.g., text + image).
                // They must be grouped into a single OpenAI message with multiple content parts.
                var contentParts = new List<OpenAI.Chat.ChatMessageContentPart>();
                var toolCalls = new List<OpenAI.Chat.ChatToolCall>();

                foreach (var content in msg.Contents)
                {
                    switch (content)
                    {
                        case TextContent textContent:
                            contentParts.Add(OpenAI.Chat.ChatMessageContentPart.CreateTextPart(
                                textContent.Text ?? string.Empty));
                            break;

                        case DataContent dataContent:
                            HandleDataContent(dataContent, contentParts);
                            break;

                        case FunctionCallContent funcCall:
                            toolCalls.Add(OpenAI.Chat.ChatToolCall.CreateFunctionToolCall(
                                funcCall.CallId ?? Guid.NewGuid().ToString(),
                                funcCall.Name,
                                BinaryData.FromObjectAsJson(funcCall.Arguments)));
                            break;

                        case FunctionResultContent funcResult:
                            openAiMessages.Add(OpenAI.Chat.ChatMessage.CreateToolMessage(
                                funcResult.CallId ?? Guid.NewGuid().ToString(),
                                funcResult.Result?.ToString() ?? ""));
                            break;
                    }
                }

                // Emit grouped content parts as a single message of the appropriate role
                if (contentParts.Count > 0)
                {
                    if (msg.Role == ChatRole.User)
                    {
                        // User messages support multi-part (text + images)
                        openAiMessages.Add(OpenAI.Chat.ChatMessage.CreateUserMessage(contentParts));
                    }
                    else if (msg.Role == ChatRole.System)
                    {
                        // System messages only support text — concatenate all text parts
                        var systemText = string.Join("\n", contentParts
                            .Select(p => p.Text)
                            .Where(t => !string.IsNullOrEmpty(t)));
                        openAiMessages.Add(OpenAI.Chat.ChatMessage.CreateSystemMessage(systemText));
                    }
                    else if (msg.Role == ChatRole.Assistant && toolCalls.Count == 0)
                    {
                        // Assistant text-only messages
                        var assistantText = string.Join("", contentParts
                            .Select(p => p.Text)
                            .Where(t => !string.IsNullOrEmpty(t)));
                        openAiMessages.Add(OpenAI.Chat.ChatMessage.CreateAssistantMessage(assistantText));
                    }
                }

                if (toolCalls.Count > 0)
                {
                    openAiMessages.Add(OpenAI.Chat.ChatMessage.CreateAssistantMessage(toolCalls));
                }
            }
            else
            {
                openAiMessages.Add(CreateOpenAIMessage(msg.Role.Value, msg.Text ?? string.Empty));
            }
        }

        return openAiMessages;
    }

    /// <summary>
    /// Converts a DataContent to OpenAI ChatMessageContentPart.
    /// Images → native vision support. Text-based files → decoded UTF-8. Binary → skipped with note.
    /// </summary>
    private void HandleDataContent(DataContent dataContent, List<OpenAI.Chat.ChatMessageContentPart> contentParts)
    {
        var mediaType = dataContent.MediaType ?? "application/octet-stream";
        var fileName = dataContent.AdditionalProperties?.TryGetValue("name", out var nameObj) == true
            ? nameObj?.ToString()
            : null;
        var label = fileName ?? mediaType;

        // Images → send as native image part (GPT-4o vision)
        if (mediaType.StartsWith("image/"))
        {
            contentParts.Add(OpenAI.Chat.ChatMessageContentPart.CreateImagePart(
                BinaryData.FromBytes(dataContent.Data.ToArray()), mediaType));
            return;
        }

        // Text-based files → decode UTF-8 and send as text
        if (IsTextBased(mediaType))
        {
            var text = System.Text.Encoding.UTF8.GetString(dataContent.Data.Span);
            contentParts.Add(OpenAI.Chat.ChatMessageContentPart.CreateTextPart(
                $"\n--- {label} ---\n{text}\n--- end ---\n"));
            return;
        }

        // Binary files (PDF, audio, video, etc.) → can't be sent as text, add metadata note
        contentParts.Add(OpenAI.Chat.ChatMessageContentPart.CreateTextPart(
            $"[Attached binary file: {label}, {dataContent.Data.Length} bytes, type: {mediaType} — this SDK version does not support native file upload, please send text-based formats like .txt, .md, .json, .csv, .xml]"));
    }

    private static bool IsTextBased(string mediaType)
        => mediaType.StartsWith("text/")
        || mediaType is "application/json" or "application/xml" or "application/javascript"
           or "application/x-yaml" or "application/yaml" or "application/x-sh"
        || mediaType.EndsWith("+json") || mediaType.EndsWith("+xml");

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
