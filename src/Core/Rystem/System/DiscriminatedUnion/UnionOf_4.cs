using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class UnionOf<T0, T1, T2, T3>(object? value) : UnionOf<T0, T1, T2>(value)
    {
        public T3? AsT3 => TryGet<T3>(3);
        private protected override int MaxIndex => 4;
        private protected override bool SetWrappers(object? value)
        {
            if (base.SetWrappers(value))
                return true;
            else if (Set<T3>(3, value))
                return true;
            return false;
        }
        public static implicit operator UnionOf<T0, T1, T2, T3>(T0 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3>(T1 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3>(T2 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2, T3>(T3 entity) => new(entity);
    }
}
