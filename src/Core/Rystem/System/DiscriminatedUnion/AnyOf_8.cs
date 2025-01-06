using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class AnyOf<T0, T1, T2, T3, T4, T5, T6, T7> : AnyOf<T0, T1, T2, T3, T4, T5, T6>
    {
        public T7? AsT7 => TryGet<T7>(7);
        public T7 CastT7 => Get<T7>(7);
        public bool IsT7 => Is<T7>();
        private protected override int NumberOfElements => 8;
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
            else if (Set<T7>(7, value))
                return true;
            return false;
        }
        public override Type? GetCurrentType()
        {
            var type = base.GetCurrentType();
            if (Index == 7)
                return typeof(T7);
            return type;
        }
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7>(T0 entity) => new(entity, 0);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7>(T1 entity) => new(entity, 1);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7>(T2 entity) => new(entity, 2);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7>(T3 entity) => new(entity, 3);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7>(T4 entity) => new(entity, 4);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7>(T5 entity) => new(entity, 5);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7>(T6 entity) => new(entity, 6);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7>(T7 entity) => new(entity, 7);
    }
}
