using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class AnyOf<T0, T1, T2, T3, T4, T5, T6> : AnyOf<T0, T1, T2, T3, T4, T5>
    {
        public T6? AsT6 => TryGet<T6>(6);
        public T6 CastT6 => Get<T6>(6);
        private protected override int NumberOfElements => 7;
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
            else if (Set<T6>(6, value))
                return true;
            return false;
        }
        public override Type? GetCurrentType()
        {
            var type = base.GetCurrentType();
            if (Index == 6)
                return typeof(T6);
            return type;
        }
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T0 entity) => new(entity, 0);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T1 entity) => new(entity, 1);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T2 entity) => new(entity, 2);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T3 entity) => new(entity, 3);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T4 entity) => new(entity, 4);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T5 entity) => new(entity, 5);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6>(T6 entity) => new(entity, 6);
    }
}
