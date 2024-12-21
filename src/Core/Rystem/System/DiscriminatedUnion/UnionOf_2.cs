using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class UnionOf<T0, T1> : IUnionOf
    {
        private Wrapper[]? _wrappers;
        public int Index { get; private protected set; } = -1;
        public T0? AsT0 => TryGet<T0>(0);
        public T1? AsT1 => TryGet<T1>(1);
        private protected virtual int MaxIndex => 2;
        public UnionOf(object? value)
        {
            UnionOfInstance(value);
        }
        private protected void UnionOfInstance(object? value)
        {
            _wrappers = new Wrapper[MaxIndex];
            var check = SetWrappers(value);
            if (!check)
                throw new ArgumentException($"Invalid value in UnionOf. You're passing an object of type: {value?.GetType().FullName}", nameof(value));
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
        private protected virtual bool SetWrappers(object? value)
        {
            foreach (var wrapper in _wrappers!)
            {
                if (wrapper?.Entity != null)
                    wrapper.Entity = null;
            }
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
        public T? Get<T>() => Value is T value ? value : default;
        public object? Value
        {
            get
            {
                foreach (var wrapper in _wrappers!)
                {
                    if (wrapper?.Entity != null)
                        return wrapper.Entity;
                }
                return null;
            }
            set
            {
                SetWrappers(value);
            }
        }
        public static implicit operator UnionOf<T0, T1>(T0 entity)
            => new(entity);
        public static implicit operator UnionOf<T0, T1>(T1 entity)
            => new(entity);
        public override string? ToString()
            => Value?.ToString();
        public override bool Equals(object? obj)
        {
            if (obj == null && Value == null)
                return true;
            var dynamicValue = ((dynamic)obj!).Value;
            return Value?.Equals(dynamicValue) ?? false;
        }
        public override int GetHashCode()
            => RuntimeHelpers.GetHashCode(Value);
    }
}
