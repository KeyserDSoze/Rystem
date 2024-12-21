using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class AnyOf<T0, T1, T2, T3, T4, T5, T6>(object? value) : AnyOf<T0, T1, T2, T3, T4, T5>(value)
    {
        public T6? AsT6 => TryGet<T6>(6);
        private protected override int MaxIndex => 7;
        private protected override bool SetWrappers(object? value)
        {
            if (base.SetWrappers(value))
                return true;
            else if (Set<T6>(6, value))
                return true;
            return false;
        }
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T0 entity) => new(entity);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T1 entity) => new(entity);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T2 entity) => new(entity);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T3 entity) => new(entity);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T4 entity) => new(entity);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T5 entity) => new(entity);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T6 entity) => new(entity);
    }
}
