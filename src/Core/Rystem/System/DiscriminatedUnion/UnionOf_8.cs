using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class UnionOf<T0, T1, T2, T3, T4, T5, T6, T7>(object? value) : UnionOf<T0, T1, T2, T3, T4, T5, T6>(value)
    {
        public T7? AsT7 => TryGet<T7>(7);
        private protected override int MaxIndex => 8;
        private protected override bool SetWrappers(object? value)
        {
            if (base.SetWrappers(value))
                return true;
            else if (Set<T7>(7, value))
                return true;
            return false;
        }
        public static implicit operator UnionOf<T0, T1, T2, T3, T4, T5, T6, T7>(T0 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3, T4, T5, T6, T7>(T1 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3, T4, T5, T6, T7>(T2 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3, T4, T5, T6, T7>(T3 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3, T4, T5, T6, T7>(T4 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3, T4, T5, T6, T7>(T5 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3, T4, T5, T6, T7>(T6 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3, T4, T5, T6, T7>(T7 entity) => new(entity);
    }
}
