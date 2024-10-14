using System;
using System.ClientModel;
using System.Population.Random;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;

namespace Rystem.PlayFramework
{
    public interface IChatBuilder
    {
        IChatBuilder AddConfiguration(string configurationName, Action<ChatBuilderSettings> settings);
    }
    public interface IChatClient : IChatClientBuilder, IChatClientToolBuilder
    {
        IAsyncEnumerable<ChatResponse> ExecuteStreamAsync(CancellationToken cancellationToken = default);
        Task<ChatResponse> ExecuteAsync(CancellationToken cancellationToken = default);
    }
    public interface IChatClientToolBuilder
    {
        IChatClient AddStrictTool<T>(string name, string description);
        IChatClient AddTool<T>(string name, string description);
        IChatClient AddStrictTool<T>(string name, string description, T entity);
        IChatClient AddTool<T>(string name, string description, T entity);
        IChatClient AddStrictTool(Tool tool);
        IChatClient AddTool(Tool tool);
    }
    public interface IChatClientBuilder
    {
        IChatClient AddUserMessage(string message);
        IChatClient AddSystemMessage(string message);
        IChatClient AddAssistantMessage(string message);
        IChatClient AddMaxOutputToken(int maxToken);
        IChatClient AddPriceModel(ChatPriceSettings priceSettings);
    }
    internal sealed class ChatClient : IChatClient, IChatClientToolBuilder, IChatClientBuilder
    {
        private readonly OpenAI.Chat.ChatClient _chatClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<ChatMessage> _messages = [];
        private readonly ChatCompletionOptions _chatCompletionOptions = new();
        public ChatClient(OpenAI.Chat.ChatClient chatClient, IServiceProvider serviceProvider)
        {
            _chatClient = chatClient;
            _serviceProvider = serviceProvider;
        }
        public IChatClient AddUserMessage(string message)
        {
            _messages.Add(new UserChatMessage(message));
            return this;
        }
        public IChatClient AddSystemMessage(string message)
        {
            _messages.Add(new SystemChatMessage(message));
            return this;
        }
        public IChatClient AddAssistantMessage(string message)
        {
            _messages.Add(new AssistantChatMessage(message));
            return this;
        }
        public IChatClient AddMaxOutputToken(int maxToken)
        {
            _chatCompletionOptions.MaxOutputTokenCount = maxToken;
            return this;
        }
        public IChatClient AddStrictTool(Tool tool)
          => AddTool(tool, true);
        public IChatClient AddTool(Tool tool)
            => AddTool(tool, false);
        private IChatClient AddTool(Tool tool, bool strict)
        {
            _chatCompletionOptions.ToolChoice = ChatToolChoice.CreateAutoChoice();
            _chatCompletionOptions.Tools.Add(ChatTool.CreateFunctionTool(tool.Name, tool.Description, BinaryData.FromString(tool.Parameters.ToJson()), strict));
            return this;
        }
        public IChatClient AddStrictTool<T>(string name, string description)
           => AddTool<T>(name, description, true);
        public IChatClient AddTool<T>(string name, string description)
            => AddTool<T>(name, description, false);
        private IChatClient AddTool<T>(string name, string description, bool strict)
        {
            var populationService = _serviceProvider.GetRequiredService<IPopulation<T>>();
            var item = populationService.Populate(1, 1).First();
            return AddTool(name, description, item, strict);
        }
        public IChatClient AddStrictTool<T>(string name, string description, T entity)
            => AddTool(name, description, entity, true);
        public IChatClient AddTool<T>(string name, string description, T entity)
            => AddTool(name, description, entity, false);
        private IChatClient AddTool<T>(string name, string description, T entity, bool strict)
        {
            _chatCompletionOptions.ToolChoice = ChatToolChoice.CreateAutoChoice();
            _chatCompletionOptions.Tools.Add(ChatTool.CreateFunctionTool(name, description, BinaryData.FromObjectAsJson(entity), strict));
            return this;
        }
        private ChatPriceSettings? _priceSettings;
        public IChatClient AddPriceModel(ChatPriceSettings priceSettings)
        {
            _priceSettings = priceSettings;
            return this;
        }
        public async IAsyncEnumerable<ChatResponse> ExecuteStreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var response = new ChatResponse();
            var requests = _chatClient.CompleteChatStreamingAsync(_messages, _chatCompletionOptions, cancellationToken);
            await foreach (var chatUpdate in requests)
            {
                if (chatUpdate.FinishReason != null)
                {
                    response.FinishReason = chatUpdate.FinishReason.Value;
                    break;
                }
                else
                {
                    foreach (var contentPart in chatUpdate.ContentUpdate)
                    {
                        response.LastChunk = contentPart.Text;
                        response.FullTextStringBuilder.Append(contentPart.Text);
                        yield return response;
                    }
                }
                if (chatUpdate.Usage != null)
                {
                    response.Price = new ChatPrice()
                    {
                        InputToken = chatUpdate.Usage.InputTokenCount,
                        OutputToken = chatUpdate.Usage.OutputTokenCount,
                        InputPrice = _priceSettings != null ? chatUpdate.Usage.InputTokenCount * _priceSettings.InputToken : 0,
                        OutputPrice = _priceSettings != null ? chatUpdate.Usage.OutputTokenCount * _priceSettings.OutputToken : 0
                    };
                    yield return response;
                }
            }
            response.FullText = response.FullTextStringBuilder.ToString();
            yield return response;
        }
        public async Task<ChatResponse> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var response = new ChatResponse();
            var requests = await _chatClient.CompleteChatAsync(_messages, _chatCompletionOptions, cancellationToken);
            var chatUpdate = requests.Value;
            if (chatUpdate.Usage != null)
            {
                response.Price = new ChatPrice()
                {
                    InputToken = chatUpdate.Usage.InputTokenCount,
                    OutputToken = chatUpdate.Usage.OutputTokenCount,
                    InputPrice = _priceSettings != null ? chatUpdate.Usage.InputTokenCount * _priceSettings.InputToken : 0,
                    OutputPrice = _priceSettings != null ? chatUpdate.Usage.OutputTokenCount * _priceSettings.OutputToken : 0
                };
            }
            response.FinishReason = chatUpdate.FinishReason;
            foreach (var content in chatUpdate.Content)
            {
                response.LastChunk = content.Text;
                response.FullTextStringBuilder.Append(content.Text);
            }
            foreach (var contentTool in chatUpdate.ToolCalls)
            {
                if (response.ToolCalls == null)
                    response.ToolCalls = [];
                response.ToolCalls.Add(new ToolCall
                {
                    FunctionName = contentTool.FunctionName,
                    Entity = contentTool.FunctionArguments,
                    Kind = contentTool.Kind,
                    Id = contentTool.Id
                });
            }
            response.FullText = response.FullTextStringBuilder.ToString();
            return response;
        }
    }
    public sealed class ChatResponse
    {
        public string? LastChunk { get; set; }
        public string? FullText { get; set; }
        public ChatFinishReason FinishReason { get; set; }
        public bool NeedToolCall => FinishReason == ChatFinishReason.ToolCalls;
        public bool NeedFunctionCall => FinishReason == ChatFinishReason.FunctionCall;
        public bool NeedFunctionCallOrToolCall => NeedToolCall || NeedFunctionCall;
        public bool ProblemWithContent => FinishReason == ChatFinishReason.ContentFilter;
        public bool LengthReached => FinishReason == ChatFinishReason.Length;
        public bool HasNormallyEnded => FinishReason == ChatFinishReason.Stop;
        public bool HasEnded => NeedToolCall || NeedFunctionCall || ProblemWithContent || LengthReached || HasNormallyEnded;
        [JsonIgnore]
        public StringBuilder FullTextStringBuilder { get; set; } = new();
        public ChatPrice? Price { get; set; }
        public List<ToolCall>? ToolCalls { get; set; }
    }
    public sealed class ToolCall
    {
        public string Id { get; set; }
        public string FunctionName { get; set; }
        public BinaryData Entity { get; set; }
        public ChatToolCallKind Kind { get; set; }
    }
    public sealed class ChatPriceSettings
    {
        public decimal InputToken { get; set; }
        public decimal OutputToken { get; set; }
    }
    public sealed class ChatPrice
    {
        public decimal InputToken { get; set; }
        public decimal OutputToken { get; set; }
        public decimal InputPrice { get; set; }
        public decimal OutputPrice { get; set; }
        public decimal TotalPrice => InputPrice + OutputPrice;
    }
    internal sealed class ChatBuilder : IChatBuilder
    {
        private readonly IServiceCollection _services;
        public ChatBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public IChatBuilder AddConfiguration(string configurationName, Action<ChatBuilderSettings> settings)
        {
            var chatBuilderSettings = new ChatBuilderSettings();
            settings(chatBuilderSettings);
            _services.AddFactory<IChatClient>(
                ((serviceProvider, name) =>
                {
                    var openAiClient = new AzureOpenAIClient(new Uri(chatBuilderSettings.Uri), new ApiKeyCredential(chatBuilderSettings.ApiKey));
                    var chatClient = openAiClient.GetChatClient(chatBuilderSettings.Model);
                    var thisChatClient = new ChatClient(chatClient, serviceProvider);
                    chatBuilderSettings.ChatClientBuilder?.Invoke(thisChatClient);
                    return thisChatClient;
                }
            ), configurationName);
            return this;
        }
    }
    public sealed class ChatBuilderSettings
    {
        public string? Uri { get; set; }
        public string? ApiKey { get; set; }
        public string? Model { get; set; }
        public string? ManagedIdentityId { get; set; }
        public Action<IChatClientBuilder>? ChatClientBuilder { get; set; }
    }
    public sealed class Tool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
        [JsonPropertyName("description")]
        public string Description { get; set; } = null!;
        [JsonPropertyName("parameters")]
        public ToolNonPrimitiveProperty Parameters { get; set; } = null!;
    }
    public sealed class ToolNonPrimitiveProperty : ToolProperty
    {
        private const string DefaultTypeName = "object";
        public ToolNonPrimitiveProperty()
        {
            Type = DefaultTypeName;
            Properties = new Dictionary<string, ToolProperty>();
        }
        [JsonPropertyName("properties")]
        public Dictionary<string, ToolProperty> Properties { get; }
        [JsonPropertyName("required")]
        public List<string>? Required { get; private set; }
        public ToolNonPrimitiveProperty AddRequired(params string[] names)
        {
            Required ??= new List<string>();
            Required.AddRange(names);
            return this;
        }
        public ToolNonPrimitiveProperty AddEnum(string key, ToolEnumProperty property)
            => AddProperty(key, property);
        public ToolNonPrimitiveProperty AddObject(string key, ToolNonPrimitiveProperty property)
            => AddProperty(key, property);
        public ToolNonPrimitiveProperty AddPrimitive(string key, ToolProperty property)
            => AddProperty(key, property);
        public ToolNonPrimitiveProperty AddNumber(string key, ToolNumberProperty property)
            => AddProperty(key, property);
        public ToolNonPrimitiveProperty AddArray(string key, ToolArrayProperty property)
            => AddProperty(key, property);
        internal ToolNonPrimitiveProperty AddProperty<T>(string key, T property)
            where T : ToolProperty
        {
            if (!Properties.ContainsKey(key))
                Properties.Add(key, property);
            return this;
        }
    }
    public sealed class ToolEnumProperty : ToolProperty
    {
        private const string DefaultTypeName = "string";
        public ToolEnumProperty()
        {
            Type = DefaultTypeName;
        }
        [JsonPropertyName("enum")]
        public List<string>? Enums { get; set; }
    }
    [JsonDerivedType(typeof(ToolEnumProperty))]
    [JsonDerivedType(typeof(ToolNumberProperty))]
    [JsonDerivedType(typeof(ToolNonPrimitiveProperty))]
    [JsonDerivedType(typeof(ToolArrayProperty))]
    public class ToolProperty
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        private const string DefaultTypeName = "string";
        public ToolProperty()
        {
            Type = DefaultTypeName;
        }
    }
    public sealed class ToolNumberProperty : ToolProperty
    {
        private const string DefaultTypeName = "number";
        public ToolNumberProperty()
        {
            Type = DefaultTypeName;
        }
        [JsonPropertyName("multipleOf")]
        public double? MultipleOf { get; set; }
        [JsonPropertyName("minimum")]
        public double? Minimum { get; set; }
        [JsonPropertyName("maximum")]
        public double? Maximum { get; set; }
        [JsonPropertyName("exclusiveMinimum")]
        public bool? ExclusiveMinimum { get; set; }
        [JsonPropertyName("exclusiveMaximum")]
        public bool? ExclusiveMaximum { get; set; }
    }
    public sealed class ToolArrayProperty : ToolProperty
    {
        [JsonPropertyName("items")]
        public ToolProperty? Items { get; set; }
    }
}
