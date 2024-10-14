using System.Population.Random;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;

namespace Rystem.PlayFramework
{
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
}
