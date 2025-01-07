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
        public TResult? Match<TResult>(Func<T0?, TResult>? f0, Func<T1?, TResult>? f1, Func<T2?, TResult>? f2, Func<T3?, TResult>? f3)
        {
            if (Index == 3 && f3 != null)
                return f3(AsT3);
            else
                return Match(f0, f1, f2);
        }
        public Task<TResult?> MatchAsync<TResult>(Func<T0?, Task<TResult?>>? f0, Func<T1?, Task<TResult?>>? f1, Func<T2?, Task<TResult?>>? f2, Func<T3?, Task<TResult?>>? f3)
        {
            if (Index == 3 && f3 != null)
                return f3(AsT3);
            else
                return MatchAsync(f0, f1, f2);
        }
        public void Switch(Action<T0?> a0, Action<T1?> a1, Action<T2?> a2, Action<T3?> a3)
        {
            if (Index == 3 && a3 != null)
                a3(AsT3);
            else
                Switch(a0, a1, a2);
        }
        public ValueTask SwitchAsync(Func<T0?, ValueTask> a0, Func<T1?, ValueTask> a1, Func<T2?, ValueTask> a2, Func<T3?, ValueTask> a3)
        {
            if (Index == 3 && a3 != null)
                return a3(AsT3);
            else
                return SwitchAsync(a0, a1, a2);
        }
        public static implicit operator AnyOf<T0, T1, T2, T3>(T0 entity) => new(entity, 0);
        public static implicit operator AnyOf<T0, T1, T2, T3>(T1 entity) => new(entity, 1);
        public static implicit operator AnyOf<T0, T1, T2, T3>(T2 entity) => new(entity, 2);
        public static implicit operator AnyOf<T0, T1, T2, T3>(T3 entity) => new(entity, 3);
    }
}
