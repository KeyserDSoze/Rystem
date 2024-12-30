using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class AnyOf<T0, T1, T2> : AnyOf<T0, T1>
    {
        public T2? AsT2 => TryGet<T2>(2);
        public T2 CastT2 => Get<T2>(2);
        private protected override int NumberOfElements => 3;
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
            else if (Set<T2>(2, value))
                return true;
            return false;
        }
        public override Type? GetCurrentType()
        {
            var type = base.GetCurrentType();
            if (Index == 2)
                return typeof(T2);
            return type;
        }
        public static implicit operator AnyOf<T0, T1, T2>(T0 entity) => new(entity, 0);
        public static implicit operator AnyOf<T0, T1, T2>(T1 entity) => new(entity, 1);
        public static implicit operator AnyOf<T0, T1, T2>(T2 entity) => new(entity, 2);
    }
}
