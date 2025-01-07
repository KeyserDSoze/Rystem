using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class AnyOf<T0, T1> : IAnyOf
    {
        private Wrapper?[]? _wrappers;
        public int Index { get; private protected set; } = -1;
        public T0? AsT0 => TryGet<T0>(0);
        public T1? AsT1 => TryGet<T1>(1);
        public T0 CastT0 => Get<T0>(0);
        public T1 CastT1 => Get<T1>(1);
        public bool IsT0 => Is<T0>();
        public bool IsT1 => Is<T1>();
        private protected virtual int NumberOfElements => 2;
        public AnyOf(object? value)
        {
            AnyOfInstance(value);
        }
        private protected AnyOf(object? value, int index)
        {
            _wrappers = new Wrapper[NumberOfElements];
            Index = index;
            _wrappers[index] = new(value);
        }
        private protected void AnyOfInstance(object? value)
        {
            _wrappers = new Wrapper[NumberOfElements];
            var check = SetWrappers(value);
            if (!check)
                throw new ArgumentException($"Invalid value in AnyOf. You're passing an object of type: {value?.GetType().FullName}", nameof(value));
        }
        public TResult? Match<TResult>(Func<T0?, TResult>? f0, Func<T1?, TResult>? f1)
        {
            if (Index == 0 && f0 != null)
                return f0(AsT0);
            else if (Index == 1 && f1 != null)
                return f1(AsT1);
            return default;
        }
        public Task<TResult?> MatchAsync<TResult>(Func<T0?, Task<TResult?>>? f0, Func<T1?, Task<TResult?>>? f1)
        {
            if (Index == 0 && f0 != null)
                return f0(AsT0);
            else if (Index == 1 && f1 != null)
                return f1(AsT1);
            return Task.FromResult(default(TResult));
        }
        public void Switch(Action<T0?> a0, Action<T1?> a1)
        {
            if (Index == 0 && a0 != null)
                a0(AsT0);
            else if (Index == 1 && a1 != null)
                a1(AsT1);
        }
        public ValueTask SwitchAsync(Func<T0?, ValueTask> a0, Func<T1?, ValueTask> a1)
        {
            if (Index == 0 && a0 != null)
                return a0(AsT0);
            else if (Index == 1 && a1 != null)
                return a1(AsT1);
            return ValueTask.CompletedTask;
        }
        private protected Q? TryGet<Q>(int index)
        {
            if (Index != index)
                return default;
            var value = _wrappers![index];
            if (value?.Entity == null)
                return default;
            var entity = (Q)value.Entity;
            return entity;
        }
        private protected Q Get<Q>(int index)
        {
            if (Index != index)
                throw new InvalidCastException($"Cannot cast {typeof(Q).FullName} to {Value?.GetType().FullName ?? "null"}");
            var value = _wrappers![index];
            if (value?.Entity == null)
                return default!;
            var entity = (Q)value.Entity;
            return entity;
        }
        public bool Is<T>()
        {
            if (Index < 0)
                return false;
            return Value is T;
        }
        public bool Is<T>(out T? entity)
        {
            if (Index >= 0 && Value is T value)
            {
                entity = value;
                return true;
            }
            entity = default;
            return false;
        }
        private protected virtual bool SetWrappers(object? value)
        {
            for (var i = 0; i < NumberOfElements; i++)
                _wrappers![i] = default;
            Index = -1;
            if (value == null)
                return true;
            else if (Set<T0>(0, value))
                return true;
            else if (Set<T1>(1, value))
                return true;
            return false;
        }
        private protected bool Set<T>(int index, object? value)
        {
            if (value is T v)
            {
                Index = index;
                _wrappers![index] = new(v);
                return true;
            }
            return false;
        }
        public virtual Type? GetCurrentType()
        {
            if (Index == -1)
                return null;
            else if (Index == 0)
                return typeof(T0);
            else if (Index == 1)
                return typeof(T1);
            return null;
        }
        public T? Get<T>() => Value is T value ? value : default;
        public dynamic? Dynamic => Value;
        public object? Value
        {
            get
            {
                if (Index >= 0 && _wrappers![Index] != default)
                    return _wrappers![Index]!.Entity;
                else
                    foreach (var wrapper in _wrappers!)
                    {
                        if (wrapper?.Entity != default)
                            return wrapper.Entity;
                    }
                return default;
            }
            set
            {
                SetWrappers(value);
            }
        }
        public static implicit operator AnyOf<T0, T1>(T0 entity) => new(entity, 0);
        public static implicit operator AnyOf<T0, T1>(T1 entity) => new(entity, 1);
        public override string? ToString() => Value?.ToString();
        public override bool Equals(object? obj)
        {
            var value = Value;
            if (obj == default && value == default)
                return true;
            var dynamicValue = ((dynamic)obj!).Value;
            return value?.Equals(dynamicValue) ?? false;
        }
        public override int GetHashCode()
            => RuntimeHelpers.GetHashCode(Value);
    }
}
