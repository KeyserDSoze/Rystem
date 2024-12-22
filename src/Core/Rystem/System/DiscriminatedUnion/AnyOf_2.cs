﻿using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class AnyOf<T0, T1> : IAnyOf
    {
        private Wrapper[]? _wrappers;
        public int Index { get; private protected set; } = -1;
        public T0? AsT0 => TryGet<T0>(0);
        public T1? AsT1 => TryGet<T1>(1);
        public T0 CastT0 => Get<T0>(0);
        public T1 CastT1 => Get<T1>(1);
        private protected virtual int MaxIndex => 2;
        public AnyOf(object? value)
        {
            AnyOfInstance(value);
        }
        private protected AnyOf(object? value, int index)
        {
            _wrappers = new Wrapper[MaxIndex];
            Index = index;
            _wrappers[index] = new(value);
        }
        private protected void AnyOfInstance(object? value)
        {
            _wrappers = new Wrapper[MaxIndex];
            var check = SetWrappers(value);
            if (!check)
                throw new ArgumentException($"Invalid value in AnyOf. You're passing an object of type: {value?.GetType().FullName}", nameof(value));
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
        public static implicit operator AnyOf<T0, T1>(T0 entity) => new(entity, 0);
        public static implicit operator AnyOf<T0, T1>(T1 entity) => new(entity, 1);
        public override string? ToString() => Value?.ToString();
        public override bool Equals(object? obj)
        {
            if (obj == null && Value == null)
                return true;
            var dynamicValue = ((dynamic)obj!).Value;
            return Value?.Equals(dynamicValue) ?? false;
        }
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(Value);
    }
}
