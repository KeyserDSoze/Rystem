using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class AnyOf<T0, T1, T2> : AnyOf<T0, T1>
    {
        public T2? AsT2 => TryGet<T2>(2);
        public T2 CastT2 => Get<T2>(2);
        public bool IsT2 => Index == 2;
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
        public TResult? Match<TResult>(Func<T0?, TResult>? f0, Func<T1?, TResult>? f1, Func<T2?, TResult>? f2)
        {
            if (Index == 2 && f2 != null)
                return f2(AsT2);
            else
                return Match(f0, f1);
        }
        public Task<TResult?> MatchAsync<TResult>(Func<T0?, Task<TResult?>>? f0, Func<T1?, Task<TResult?>>? f1, Func<T2?, Task<TResult?>>? f2)
        {
            if (Index == 2 && f2 != null)
                return f2(AsT2);
            else
                return MatchAsync(f0, f1);
        }
        public void Switch(Action<T0?> a0, Action<T1?> a1, Action<T2?> a2)
        {
            if (Index == 2 && a2 != null)
                a2(AsT2);
            else
                Switch(a0, a1);
        }
        public ValueTask SwitchAsync(Func<T0?, ValueTask> a0, Func<T1?, ValueTask> a1, Func<T2?, ValueTask> a2)
        {
            if (Index == 2 && a2 != null)
                return a2(AsT2);
            else
                return SwitchAsync(a0, a1);
        }
        public static implicit operator AnyOf<T0, T1, T2>(T0 entity) => new(entity, 0);
        public static implicit operator AnyOf<T0, T1, T2>(T1 entity) => new(entity, 1);
        public static implicit operator AnyOf<T0, T1, T2>(T2 entity) => new(entity, 2);
    }
}
