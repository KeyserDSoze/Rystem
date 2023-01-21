using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    public class Entity<T, TKey>
        where TKey : notnull
    {
        public TKey? Key { get; set; }
        public T? Value { get; set; }
        [JsonIgnore]
        public bool HasValue => Value != null;
        [JsonIgnore]
        public bool HasKey => Key != null;
        public static Entity<T, TKey> Default(T value, TKey key)
            => new(value, key);
        public Entity(T? value = default, TKey? key = default)
        {
            Value = value;
            Key = key;
        }
        public State<T, TKey> ToOkState()
            => State.Ok(this);
        public State<T, TKey> ToNotOkState()
            => State.NotOk(this);
    }
}
