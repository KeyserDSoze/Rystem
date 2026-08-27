using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;
using Rystem.PlayFramework.Adapters;
using System.Text.Json;

namespace Rystem.PlayFramework.Test;

public sealed class OpenAIAdapterCompatibilityTests
{
    private const string ToolSchema = """
        {
          "type": "object",
          "properties": {
            "city": {
              "type": "string",
              "description": "City to search"
            }
          },
          "required": ["city"],
          "additionalProperties": false
        }
        """;

    [Fact]
    public void OfficialOpenAIAdapter_PreservesInvocableFunctionMetadataForChatCompletions()
    {
        var function = AIFunctionFactory.Create(
            (string city) => city,
            new AIFunctionFactoryOptions
            {
                Name = "search_weather",
                Description = "Searches the weather for a city"
            });

        var tool = function.AsOpenAIChatTool();

        Assert.Equal(function.Name, tool.FunctionName);
        Assert.Equal(function.Description, tool.FunctionDescription);
        Assert.NotNull(tool.FunctionParameters);
    }

    [Fact]
    public void ProductionAdapter_ConstructsResponsesAndChatClientsWithoutNetworkCalls()
    {
        using var responsesProvider = BuildServiceProvider(useResponsesApi: true);
        using var chatProvider = BuildServiceProvider(useResponsesApi: false);

        Assert.NotNull(responsesProvider.GetRequiredService<IChatClient>());
        Assert.NotNull(chatProvider.GetRequiredService<IChatClient>());
    }

    [Fact]
    public void ProductionAdapter_AllowsEntraAuthenticationWithoutAnApiKey()
    {
        var services = new ServiceCollection();
        services.AddAdapterForAzureOpenAI(settings =>
        {
            settings.Endpoint = new Uri("https://name.openai.azure.com/");
            settings.UseAzureCredential = true;
            settings.Deployment = "my-deployment";
            settings.EnableFileUpload = false;
        });

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IChatClient>());
    }

    [Fact]
    public void ProductionAdapter_RejectsApiKeyTogetherWithEntraAuthentication()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddAdapterForAzureOpenAI(settings =>
            {
                settings.Endpoint = new Uri("https://name.openai.azure.com/");
                settings.ApiKey = "test-key";
                settings.UseAzureCredential = true;
                settings.Deployment = "my-deployment";
            }));

        Assert.Contains(nameof(AdapterSettings.ApiKey), exception.Message);
    }

    [Fact]
    public void ProductionAdapter_RejectsWhitespaceApiKey()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddAdapterForAzureOpenAI(settings =>
            {
                settings.Endpoint = new Uri("https://name.openai.azure.com/");
                settings.ApiKey = " ";
                settings.Deployment = "my-deployment";
            }));

        Assert.Contains(nameof(AdapterSettings.ApiKey), exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ProductionAdapter_RejectsEmptySpeechToTextApiVersion(string apiVersion)
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddAdapterForAzureOpenAI(settings =>
            {
                settings.Endpoint = new Uri("https://name.openai.azure.com/");
                settings.ApiKey = "test-key";
                settings.Deployment = "my-deployment";
                settings.SpeechToTextApiVersion = apiVersion;
            }));

        Assert.Contains(nameof(AdapterSettings.SpeechToTextApiVersion), exception.Message);
    }

    [Fact]
    public void VoiceAdapter_RejectsApiKeyTogetherWithEntraAuthentication()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddVoiceAdapterForAzureOpenAI(settings =>
            {
                settings.Endpoint = new Uri("https://name.openai.azure.com/");
                settings.ApiKey = "test-key";
                settings.UseAzureCredential = true;
            }));

        Assert.Contains(nameof(VoiceAdapterSettings.ApiKey), exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void VoiceAdapter_RejectsEmptySpeechToTextApiVersion(string apiVersion)
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddVoiceAdapterForAzureOpenAI(settings =>
            {
                settings.Endpoint = new Uri("https://name.openai.azure.com/");
                settings.ApiKey = "test-key";
                settings.SttApiVersion = apiVersion;
            }));

        Assert.Contains(nameof(VoiceAdapterSettings.SttApiVersion), exception.Message);
    }

    private static ServiceProvider BuildServiceProvider(bool useResponsesApi)
    {
        var services = new ServiceCollection();
        services.AddAdapterForAzureOpenAI(settings =>
        {
            settings.Endpoint = new Uri("https://name.openai.azure.com/");
            settings.ApiKey = "test-key";
            settings.Deployment = "my-deployment";
            settings.UseResponsesApi = useResponsesApi;
            settings.EnableFileUpload = false;
        });
        return services.BuildServiceProvider();
    }

    [Fact]
    public void OfficialOpenAIAdapter_ConvertsDeclarationForChatCompletions()
    {
        var declaration = CreateDeclaration();

        var tool = declaration.AsOpenAIChatTool();

        Assert.Equal(declaration.Name, tool.FunctionName);
        Assert.Equal(declaration.Description, tool.FunctionDescription);
        Assert.NotNull(tool.FunctionParameters);

        using var expectedSchema = JsonDocument.Parse(ToolSchema);
        using var actualSchema = JsonDocument.Parse(tool.FunctionParameters.ToString());
        Assert.True(JsonElement.DeepEquals(expectedSchema.RootElement, actualSchema.RootElement));
    }

#pragma warning disable OPENAI001
    [Fact]
    public void OfficialOpenAIAdapter_ConvertsDeclarationForResponsesApi()
    {
        var declaration = CreateDeclaration();

        var tool = OpenAI.Responses.MicrosoftExtensionsAIResponsesExtensions
            .AsOpenAIResponseTool(declaration);

        Assert.NotNull(tool);
    }
#pragma warning restore OPENAI001

    private static AIFunctionDeclaration CreateDeclaration()
    {
        using var schema = JsonDocument.Parse(ToolSchema);
        return AIFunctionFactory.CreateDeclaration(
            "search_weather",
            "Searches the weather for a city",
            schema.RootElement.Clone());
    }
}
