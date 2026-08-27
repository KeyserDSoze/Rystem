using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework.Adapters;
using System.Text;

namespace Rystem.PlayFramework.Test.Tests;

public sealed class AzureOpenAIFilesLiveGateTests : PlayFrameworkTestBase
{
    public AzureOpenAIFilesLiveGateTests()
        : base(
            useRealAzureOpenAI: true,
            deploymentEnvironmentVariable: "AZURE_OPENAI_FILES_DEPLOYMENT")
    {
    }

    [AzureLiveFact("AZURE_OPENAI_FILES_DEPLOYMENT", RequiresDefaultDeployment = false)]
    [Trait("Category", "AzureOpenAIFiles")]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task Files_UploadsUsesAndDeletesTextFile()
    {
        var client = ServiceProvider.GetRequiredService<IChatClient>();
        var fileName = $"rystem-live-gate-{Guid.NewGuid():N}.txt";
        const string marker = "RYSTEM_FILE_GATE_7319";
        var file = new DataContent(Encoding.UTF8.GetBytes($"Verification value: {marker}"), "text/plain")
        {
            Name = fileName
        };
        using var timeout = LiveGateTestHelpers.CreateTimeout();

        try
        {
            var response = await client.GetResponseAsync(
                [new ChatMessage(ChatRole.User,
                    [new TextContent("Read the attached file and reply with only its verification value."), file])],
                cancellationToken: timeout.Token);

            Assert.Contains(marker, response.Text, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await DeleteRemoteFilesAsync(fileName);
        }
    }

    private async Task DeleteRemoteFilesAsync(string fileName)
    {
        var parent = OpenAIClientFactory.Create(AzureEndpoint!, AzureApiKey, useAzureCredential: false);
        var files = parent.GetOpenAIFileClient();
        using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var remoteFiles = await files.GetFilesAsync(cleanupTimeout.Token);
        foreach (var remoteFile in remoteFiles.Value.Where(x => x.Filename == fileName))
            await files.DeleteFileAsync(remoteFile.Id, cleanupTimeout.Token);
    }
}
