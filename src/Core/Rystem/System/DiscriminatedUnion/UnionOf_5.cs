using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class UnionOf<T0, T1, T2, T3, T4>(object? value) : UnionOf<T0, T1, T2, T3>(value)
    {
        public T4? AsT4 => TryGet<T4>(4);
        private protected override int MaxIndex => 5;
        private protected override bool SetWrappers(object? value)
        {
            if (base.SetWrappers(value))
                return true;
            else if (Set<T4>(4, value))
                return true;
            return false;
        }
        public static implicit operator UnionOf<T0, T1, T2, T3, T4>(T0 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3, T4>(T1 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3, T4>(T2 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3, T4>(T3 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3, T4>(T4 entity) => new(entity);
    }
}
