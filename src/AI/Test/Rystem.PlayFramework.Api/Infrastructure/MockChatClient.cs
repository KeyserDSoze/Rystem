using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework.Api.Infrastructure;

/// <summary>
/// Mock IChatClient implementation for demo purposes without Azure OpenAI.
/// Returns pre-defined responses for testing.
/// </summary>
public sealed class MockChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("MockChatClient", null, "mock-model");

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken);

        return new ChatResponse(
        [
            new ChatMessage(ChatRole.Assistant,
                "Hello! I'm a mock AI assistant. I can help you test the PlayFramework API. Configure Azure OpenAI credentials for real AI responses!")
        ])
        {
            ModelId = "mock-model"
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken);

        var words = new[]
        {
            "Hello! ", "I'm ", "a ", "mock ", "AI ", "assistant. ",
            "I ", "can ", "help ", "you ", "test ", "the ", "PlayFramework ",
            "API. ", "Configure ", "Azure ", "OpenAI ", "credentials ", "for ",
            "real ", "AI ", "responses!"
        };

        foreach (var word in words)
        {
            await Task.Delay(50, cancellationToken);
            yield return new ChatResponseUpdate(ChatRole.Assistant, word);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
