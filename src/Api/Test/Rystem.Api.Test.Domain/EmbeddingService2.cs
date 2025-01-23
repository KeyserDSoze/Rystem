using System.Runtime.CompilerServices;

namespace Rystem.Api.Test.Domain
{
    public sealed class EmbeddingService2 : IEmbeddingService
    {
        public ValueTask EmbedAsync(bool withOcr, Container container, string fileName, byte[] data, bool saveRawFile, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask EmbedAsync(Container container, string fileName, string data, bool saveRawFile, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<EmbeddingValue> FirstFileAsync(Container container, string message, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield return new EmbeddingValue() { Path = "path2", Text = "text2", Value = [3.0f, 4.0f] };
            yield return new EmbeddingValue() { Path = "path2", Text = "text2", Value = [3.0f, 4.0f] };
            yield return new EmbeddingValue() { Path = "path2", Text = "text2", Value = [3.0f, 4.0f] };
            yield return new EmbeddingValue() { Path = "path2", Text = "text2", Value = [3.0f, 4.0f] };
            yield return new EmbeddingValue() { Path = "path2", Text = "text2", Value = [3.0f, 4.0f] };
        }

        public ValueTask<float[]> GetEmbeddingsAsync(string text, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new float[] { 3.0f, 4.0f });
        }

        public async IAsyncEnumerable<EmbeddingValue> SearchAsync(Container container, string message, int take, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield return new EmbeddingValue() { Path = "path2", Text = "text2", Value = [4.0f, 6.0f] };
        }
    }
}
