using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework.Adapters;
using System.Text.Json;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Shared helpers for the Azure OpenAI live-gate test suites (<c>Tests/AzureOpenAI*LiveGateTests.cs</c>),
/// extracted so those test classes do not need to reach into each other as ad-hoc utility providers.
/// </summary>
internal static class LiveGateTestHelpers
{
    /// <summary>Generous timeout for a single live call against Azure OpenAI.</summary>
    public static CancellationTokenSource CreateTimeout() => new(TimeSpan.FromMinutes(2));

    /// <summary>
    /// Builds <see cref="ChatOptions"/> that force the model to call a single "live_gate" tool,
    /// used to verify function/tool calling end to end against the production adapter.
    /// </summary>
    public static ChatOptions CreateToolOptions()
    {
        using var schema = JsonDocument.Parse(
            """
            {"type":"object","properties":{"value":{"type":"string"}},"required":["value"],"additionalProperties":false}
            """);
        var tool = AIFunctionFactory.CreateDeclaration(
            "live_gate",
            "Returns the supplied live-gate verification value.",
            schema.RootElement.Clone());
        return new ChatOptions
        {
            Tools = [tool],
            ToolMode = ChatToolMode.RequireSpecific(tool.Name)
        };
    }

    /// <summary>
    /// Builds a standalone service provider registering an Azure OpenAI-backed <see cref="IVoiceAdapter"/>,
    /// used by the voice and speech-to-text-wrapper live gates.
    /// </summary>
    public static ServiceProvider BuildVoiceProvider(Uri endpoint, string apiKey)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVoiceAdapterForAzureOpenAI(settings =>
        {
            settings.Endpoint = endpoint;
            settings.ApiKey = apiKey;
            settings.SttDeployment = AzureLiveTestConfiguration.Resolve("AZURE_OPENAI_STT_DEPLOYMENT")!;
            settings.TtsDeployment = AzureLiveTestConfiguration.Resolve("AZURE_OPENAI_TTS_DEPLOYMENT")!;
            settings.TtsOutputFormat = "mp3";
        });
        return services.BuildServiceProvider();
    }
}
