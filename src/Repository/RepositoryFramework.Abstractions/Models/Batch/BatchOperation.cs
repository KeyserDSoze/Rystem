using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    public sealed class BatchOperation<T, TKey>
        where TKey : notnull
    {
        [JsonPropertyName("c")]
        public CommandType Command { get; }
        [JsonPropertyName("k")]
        public TKey Key { get; }
        [JsonPropertyName("v")]
        public T? Value { get; }
        public BatchOperation(CommandType command, TKey key, T? value = default)
        {
            Command = command;
            Key = key;
            Value = value;
        }
    }
}
