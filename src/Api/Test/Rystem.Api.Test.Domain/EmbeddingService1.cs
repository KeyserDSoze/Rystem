using System.Runtime.CompilerServices;

namespace Rystem.Api.Test.Domain
{
    public sealed class EmbeddingService1 : IEmbeddingService
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
            yield return new EmbeddingValue() { Path = "path", Text = "text", Value = [1.0f, 2.0f] };
            yield return new EmbeddingValue() { Path = "path", Text = "text", Value = [1.0f, 2.0f] };
            yield return new EmbeddingValue() { Path = "path", Text = "text", Value = [1.0f, 2.0f] };
            yield return new EmbeddingValue() { Path = "path", Text = "text", Value = [1.0f, 2.0f] };
            yield return new EmbeddingValue() { Path = "path", Text = "text", Value = [1.0f, 2.0f] };
        }

        public ValueTask<float[]> GetEmbeddingsAsync(string text, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new float[] { 1.0f, 2.0f });
        }

        public async IAsyncEnumerable<EmbeddingValue> SearchAsync(Container container, string message, int take, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield return new EmbeddingValue() { Path = "path", Text = "text", Value = [1.0f, 2.0f] };
            yield return new EmbeddingValue() { Path = "path", Text = "text", Value = [1.0f, 2.0f] };
            yield return new EmbeddingValue() { Path = "path", Text = "text", Value = [1.0f, 2.0f] };
            yield return new EmbeddingValue() { Path = "path", Text = "text", Value = [1.0f, 2.0f] };
            yield return new EmbeddingValue() { Path = "path", Text = "text", Value = [1.0f, 2.0f] };
        }
    }
}
