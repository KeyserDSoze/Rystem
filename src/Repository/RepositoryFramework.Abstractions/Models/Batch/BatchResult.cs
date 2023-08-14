using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    public sealed class BatchResult<T, TKey>
        where TKey : notnull
    {
        [JsonPropertyName("c")]
        public CommandType Command { get; }
        [JsonPropertyName("k")]
        public TKey Key { get; }
        [JsonPropertyName("s")]
        public State<T, TKey> State { get; }
        public BatchResult(CommandType command, TKey key, State<T, TKey> state)
        {
            Command = command;
            Key = key;
            State = state;
        }
        public static BatchResult<T, TKey> CreateInsert(TKey key, State<T, TKey> state)
            => new(CommandType.Insert, key, state);
        public static BatchResult<T, TKey> CreateUpdate(TKey key, State<T, TKey> state)
            => new(CommandType.Update, key, state);
        public static BatchResult<T, TKey> CreateDelete(TKey key, State<T, TKey> state)
            => new(CommandType.Delete, key, state);
    }
}
