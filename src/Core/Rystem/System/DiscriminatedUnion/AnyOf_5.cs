using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class AnyOf<T0, T1, T2, T3, T4> : AnyOf<T0, T1, T2, T3>
    {
        public T4? AsT4 => TryGet<T4>(4);
        public T4 CastT4 => Get<T4>(4);
        private protected override int NumberOfElements => 5;
        public AnyOf(object? value) : base(value)
        {
        }
        private protected AnyOf(object? value, int index) : base(value, index)
        {
        }
        private protected override bool SetWrappers(object? value)
        {
            if (base.SetWrappers(value))
                return true;
            else if (Set<T4>(4, value))
                return true;
            return false;
        }
        public override Type? GetCurrentType()
        {
            var type = base.GetCurrentType();
            if (Index == 4)
                return typeof(T4);
            return type;
        }
        public static implicit operator AnyOf<T0, T1, T2, T3, T4>(T0 entity) => new(entity, 0);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4>(T1 entity) => new(entity, 1);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4>(T2 entity) => new(entity, 2);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4>(T3 entity) => new(entity, 3);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4>(T4 entity) => new(entity, 4);
    }
}
