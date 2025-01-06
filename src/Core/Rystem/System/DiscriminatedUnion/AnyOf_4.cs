using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class AnyOf<T0, T1, T2, T3> : AnyOf<T0, T1, T2>
    {
        public T3? AsT3 => TryGet<T3>(3);
        public T3 CastT3 => Get<T3>(3);
        public bool IsT3 => Is<T3>();
        private protected override int NumberOfElements => 4;
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
            else if (Set<T3>(3, value))
                return true;
            return false;
        }
        public override Type? GetCurrentType()
        {
            var type = base.GetCurrentType();
            if (Index == 3)
                return typeof(T3);
            return type;
        }
        public static implicit operator AnyOf<T0, T1, T2, T3>(T0 entity) => new(entity, 0);
        public static implicit operator AnyOf<T0, T1, T2, T3>(T1 entity) => new(entity, 1);
        public static implicit operator AnyOf<T0, T1, T2, T3>(T2 entity) => new(entity, 2);
        public static implicit operator AnyOf<T0, T1, T2, T3>(T3 entity) => new(entity, 3);
    }
}
