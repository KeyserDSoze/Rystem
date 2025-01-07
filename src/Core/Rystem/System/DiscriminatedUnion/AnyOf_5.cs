using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class AnyOf<T0, T1, T2, T3, T4> : AnyOf<T0, T1, T2, T3>
    {
        public T4? AsT4 => TryGet<T4>(4);
        public T4 CastT4 => Get<T4>(4);
        public bool IsT4 => Is<T4>();
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
        public TResult? Match<TResult>(Func<T0?, TResult>? f0, Func<T1?, TResult>? f1, Func<T2?, TResult>? f2, Func<T3?, TResult>? f3,
            Func<T4?, TResult>? f4)
        {
            if (Index == 4 && f4 != null)
                return f4(AsT4);
            else
                return Match(f0, f1, f2, f3);
        }
        public Task<TResult?> MatchAsync<TResult>(Func<T0?, Task<TResult?>>? f0, Func<T1?, Task<TResult?>>? f1, Func<T2?, Task<TResult?>>? f2, Func<T3?, Task<TResult?>>? f3,
            Func<T4?, Task<TResult?>>? f4)
        {
            if (Index == 4 && f4 != null)
                return f4(AsT4);
            else
                return MatchAsync(f0, f1, f2, f3);
        }
        public void Switch(Action<T0?> a0, Action<T1?> a1, Action<T2?> a2, Action<T3?> a3, Action<T4?> a4)
        {
            if (Index == 4 && a4 != null)
                a4(AsT4);
            else
                Switch(a0, a1, a2, a3);
        }
        public ValueTask SwitchAsync(Func<T0?, ValueTask> a0, Func<T1?, ValueTask> a1, Func<T2?, ValueTask> a2, Func<T3?, ValueTask> a3,
            Func<T4?, ValueTask> a4)
        {
            if (Index == 4 && a4 != null)
                return a4(AsT4);
            else
                return SwitchAsync(a0, a1, a2, a3);
        }
        public static implicit operator AnyOf<T0, T1, T2, T3, T4>(T0 entity) => new(entity, 0);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4>(T1 entity) => new(entity, 1);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4>(T2 entity) => new(entity, 2);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4>(T3 entity) => new(entity, 3);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4>(T4 entity) => new(entity, 4);
    }
}
