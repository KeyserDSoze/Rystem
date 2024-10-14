using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework
{
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
}
