using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

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
            if (chatBuilderSettings.Uri == null)
                throw new ArgumentNullException(nameof(chatBuilderSettings.Uri));
            AzureOpenAIClient? openAiClient;
            if (chatBuilderSettings.UseSystemManagedIdentity)
                openAiClient = new AzureOpenAIClient(new Uri(chatBuilderSettings.Uri!), new ManagedIdentityCredential());
            else if (chatBuilderSettings.ManagedIdentityId != null)
                openAiClient = new AzureOpenAIClient(new Uri(chatBuilderSettings.Uri!), new ManagedIdentityCredential(chatBuilderSettings.ManagedIdentityId!));
            else if (chatBuilderSettings.ApiKey != null)
                openAiClient = new AzureOpenAIClient(new Uri(chatBuilderSettings.Uri!), new ApiKeyCredential(chatBuilderSettings.ApiKey!));
            else
                throw new ArgumentNullException($"{nameof(chatBuilderSettings.ApiKey)} or {nameof(chatBuilderSettings.ManagedIdentityId)} or {nameof(chatBuilderSettings.UseSystemManagedIdentity)} needs to be present.");
            _services.AddFactory<IChatClient>(
                (serviceProvider, name) =>
                {
                    var chatClient = openAiClient.GetChatClient(chatBuilderSettings.Model);
                    var thisChatClient = new ChatClient(chatClient, serviceProvider);
                    chatBuilderSettings.ChatClientBuilder?.Invoke(thisChatClient);
                    return thisChatClient;
                }, configurationName);
            return this;
        }
    }
}
