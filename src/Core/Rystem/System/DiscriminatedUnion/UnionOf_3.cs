using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class UnionOf<T0, T1, T2>(object? value) : UnionOf<T0, T1>(value)
    {
        public T2? AsT2 => TryGet<T2>(2);
        private protected override int MaxIndex => 3;
        private protected override bool SetWrappers(object? value)
        {
            if (base.SetWrappers(value))
                return true;
            else if (Set<T2>(2, value))
                return true;
            return false;
        }
        public static implicit operator UnionOf<T0, T1, T2>(T0 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2>(T1 entity) => new(entity);
        public static implicit operator UnionOf<T0, T1, T2>(T2 entity) => new(entity);
    }
}
