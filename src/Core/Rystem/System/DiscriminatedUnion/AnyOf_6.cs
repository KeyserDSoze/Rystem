using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class AnyOf<T0, T1, T2, T3, T4, T5>(object? value) : AnyOf<T0, T1, T2, T3, T4>(value)
    {
        public T5? AsT5 => TryGet<T5>(5);
        private protected override int MaxIndex => 6;
        private protected override bool SetWrappers(object? value)
        {
            if (base.SetWrappers(value))
                return true;
            else if (Set<T5>(5, value))
                return true;
            return false;
        }
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5>(T0 entity) => new(entity);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5>(T1 entity) => new(entity);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5>(T2 entity) => new(entity);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5>(T3 entity) => new(entity);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5>(T4 entity) => new(entity);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5>(T5 entity) => new(entity);
    }
}
