using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using System.Text.Json;
using AIMessage = Microsoft.Extensions.AI.ChatMessage;
using AIResponse = Microsoft.Extensions.AI.ChatResponse;
using AIUpdate = Microsoft.Extensions.AI.ChatResponseUpdate;
using OpenAIMessage = OpenAI.Chat.ChatMessage;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Adapter for Azure OpenAI that implements IChatClient from Microsoft.Extensions.AI.
/// Supports function/tool calling.
/// </summary>
public sealed class AzureOpenAIChatClientAdapter : IChatClient
{
    private readonly Azure.AI.OpenAI.AzureOpenAIClient _azureClient;
    private readonly OpenAI.Chat.ChatClient _chatClient;
    private readonly string _deploymentName;

    public AzureOpenAIChatClientAdapter(string endpoint, string apiKey, string deploymentName)
    {
        _deploymentName = deploymentName;
        _azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _chatClient = _azureClient.GetChatClient(deploymentName);
    }

    public ChatClientMetadata Metadata => new(
        providerName: "AzureOpenAI",
        providerUri: new Uri("https://azure.microsoft.com/products/ai-services/openai-service"),
        _deploymentName);

    public async Task<AIResponse> GetResponseAsync(
        IEnumerable<AIMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Convert AI messages to OpenAI messages
        var openAiMessages = new List<OpenAIMessage>();
        foreach (var msg in messages)
        {
            var text = msg.Text ?? string.Empty;
            openAiMessages.Add(msg.Role.Value switch
            {
                "system" => OpenAIMessage.CreateSystemMessage(text),
                "user" => OpenAIMessage.CreateUserMessage(text),
                "assistant" => OpenAIMessage.CreateAssistantMessage(text),
                _ => OpenAIMessage.CreateUserMessage(text)
            });
        }

        // Build options
        var chatOptions = new OpenAI.Chat.ChatCompletionOptions();
        if (options?.Temperature.HasValue == true)
            chatOptions.Temperature = options.Temperature.Value;
        if (options?.MaxOutputTokens.HasValue == true)
            chatOptions.MaxOutputTokenCount = options.MaxOutputTokens.Value;

        // Add tools if provided
        if (options?.Tools != null && options.Tools.Count > 0)
        {
            foreach (var tool in options.Tools)
            {
                if (tool is AIFunction aiFunction)
                {
                    var functionDef = ConvertToOpenAITool(aiFunction);
                    chatOptions.Tools.Add(functionDef);
                }
            }
        }

        // Call Azure OpenAI
        var completion = await _chatClient.CompleteChatAsync(openAiMessages, chatOptions, cancellationToken);

        // Convert response
        return ConvertToAIResponse(completion.Value);
    }

    public async IAsyncEnumerable<AIUpdate> GetStreamingResponseAsync(
        IEnumerable<AIMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Convert messages
        var openAiMessages = new List<OpenAIMessage>();
        foreach (var msg in messages)
        {
            var text = msg.Text ?? string.Empty;
            openAiMessages.Add(msg.Role.Value switch
            {
                "system" => OpenAIMessage.CreateSystemMessage(text),
                "user" => OpenAIMessage.CreateUserMessage(text),
                "assistant" => OpenAIMessage.CreateAssistantMessage(text),
                _ => OpenAIMessage.CreateUserMessage(text)
            });
        }

        // Build options
        var chatOptions = new OpenAI.Chat.ChatCompletionOptions();
        if (options?.Temperature.HasValue == true)
            chatOptions.Temperature = options.Temperature.Value;
        if (options?.MaxOutputTokens.HasValue == true)
            chatOptions.MaxOutputTokenCount = options.MaxOutputTokens.Value;

        // Stream response
        await foreach (var streamUpdate in _chatClient.CompleteChatStreamingAsync(openAiMessages, chatOptions, cancellationToken))
        {
            foreach (var contentPart in streamUpdate.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentPart.Text))
                {
                    yield return new AIUpdate(ChatRole.Assistant, contentPart.Text);
                }
            }
        }
    }

    private static AIResponse ConvertToAIResponse(OpenAI.Chat.ChatCompletion completion)
    {
        var responseMessage = new AIMessage(ChatRole.Assistant, string.Empty);

        // Add text content
        if (completion.Content.Count > 0)
        {
            var textContent = string.Join("", completion.Content.Select(c => c.Text));
            responseMessage = new AIMessage(ChatRole.Assistant, textContent);
        }

        // Add function calls if present
        if (completion.ToolCalls?.Count > 0)
        {
            responseMessage.Contents.Clear(); // Clear default text content

            foreach (var toolCall in completion.ToolCalls)
            {
                if (toolCall is OpenAI.Chat.ChatToolCall chatToolCall)
                {
                    // Parse arguments to dictionary
                    var argsDict = new Dictionary<string, object?>();
                    if (!string.IsNullOrEmpty(chatToolCall.FunctionArguments.ToString()))
                    {
                        try
                        {
                            var parsedArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                                chatToolCall.FunctionArguments.ToString());

                            if (parsedArgs != null)
                            {
                                foreach (var kvp in parsedArgs)
                                {
                                    argsDict[kvp.Key] = kvp.Value.ValueKind switch
                                    {
                                        JsonValueKind.String => kvp.Value.GetString(),
                                        JsonValueKind.Number => kvp.Value.GetDouble(),
                                        JsonValueKind.True => true,
                                        JsonValueKind.False => false,
                                        JsonValueKind.Null => null,
                                        _ => kvp.Value.ToString()
                                    };
                                }
                            }
                        }
                        catch
                        {
                            // Fallback: use raw string
                            argsDict["raw"] = chatToolCall.FunctionArguments.ToString();
                        }
                    }

                    var functionCall = new FunctionCallContent(
                        chatToolCall.Id,
                        chatToolCall.FunctionName,
                        argsDict);

                    responseMessage.Contents.Add(functionCall);
                }
            }
        }

        var response = new AIResponse(responseMessage)
        {
            ModelId = completion.Model
        };

        // Add usage information
        if (completion.Usage != null)
        {
            response.Usage = new UsageDetails
            {
                InputTokenCount = completion.Usage.InputTokenCount,
                OutputTokenCount = completion.Usage.OutputTokenCount,
                TotalTokenCount = completion.Usage.TotalTokenCount
            };
        }

        return response;
    }

    private static OpenAI.Chat.ChatTool ConvertToOpenAITool(AIFunction function)
    {
        // Build function parameters schema
        var parameters = new
        {
            type = "object",
            properties = new Dictionary<string, object>(),
            required = new List<string>()
        };

        // Note: For now, we create a simple schema
        // You may need to enhance this based on your AIFunction metadata
        var functionDef = OpenAI.Chat.ChatTool.CreateFunctionTool(
            functionName: function.Name,
            functionDescription: function.Description);

        return functionDef;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(Azure.AI.OpenAI.AzureOpenAIClient))
            return _azureClient;
        if (serviceType == typeof(OpenAI.Chat.ChatClient))
            return _chatClient;
        return null;
    }

    public void Dispose()
    {
        // Azure client doesn't require explicit disposal
    }
}

