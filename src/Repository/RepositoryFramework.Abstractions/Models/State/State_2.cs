using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    public class State<T, TKey>
        where TKey : notnull
    {
        public bool IsOk { get; set; }
        public Entity<T, TKey>? Entity { get; set; }
        public int? Code { get; set; }
        public string? Message { get; set; }
        [JsonIgnore]
        public bool HasEntity => Entity?.HasValue == true;
        public State(bool isOk, T? value = default, TKey? key = default, int? code = default, string? message = default)
            : this(isOk, value != null || key != null ? new Entity<T, TKey>(value!, key) : default, code, message)
        {
        }
        [JsonConstructor]
        public State(bool isOk, Entity<T, TKey>? entity, int? code = default, string? message = default)
        {
            IsOk = isOk;
            if (entity != null)
                Entity = entity;
            Code = code;
            Message = message;
        }
        public static State<T, TKey> Ok() => new(true);
        public static State<T, TKey> NotOk() => new(false);

        public static implicit operator bool(State<T, TKey> state)
            => state.IsOk;
        public static implicit operator State<T, TKey>(bool state)
            => new(state);
        public static implicit operator int(State<T, TKey> state)
            => state.Code ?? 0;
        public static implicit operator State<T, TKey>(int code)
            => new(false, default, code);
    }
}
