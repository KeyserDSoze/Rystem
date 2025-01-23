namespace Rystem.Api.Test.Domain
{
    public interface IEmbeddingService
    {
        IAsyncEnumerable<EmbeddingValue> SearchAsync(Container container, string message, int take, CancellationToken cancellationToken);
        IAsyncEnumerable<EmbeddingValue> FirstFileAsync(Container container, string message, CancellationToken cancellationToken);
        ValueTask<float[]> GetEmbeddingsAsync(string text, CancellationToken cancellationToken);
        ValueTask EmbedAsync(bool withOcr, Container container, string fileName, byte[] data, bool saveRawFile, CancellationToken cancellationToken);
        ValueTask EmbedAsync(Container container, string fileName, string data, bool saveRawFile, CancellationToken cancellationToken);
    }
}
