using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class AnyOf<T0, T1, T2, T3, T4, T5, T6, T7> : AnyOf<T0, T1, T2, T3, T4, T5, T6>
    {
        public T7? AsT7 => TryGet<T7>(7);
        public T7 CastT7 => Get<T7>(7);
        public bool IsT7 => Index == 7;
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
        public TResult? Match<TResult>(Func<T0?, TResult>? f0, Func<T1?, TResult>? f1, Func<T2?, TResult>? f2, Func<T3?, TResult>? f3,
        Func<T4?, TResult>? f4, Func<T5?, TResult>? f5, Func<T6?, TResult>? f6, Func<T7?, TResult>? f7)
        {
            if (Index == 7 && f7 != null)
                return f7(AsT7);
            else
                return Match(f0, f1, f2, f3, f4, f5, f6);
        }
        public Task<TResult?> MatchAsync<TResult>(Func<T0?, Task<TResult?>>? f0, Func<T1?, Task<TResult?>>? f1, Func<T2?, Task<TResult?>>? f2, Func<T3?, Task<TResult?>>? f3,
            Func<T4?, Task<TResult?>>? f4, Func<T5?, Task<TResult?>>? f5, Func<T6?, Task<TResult?>>? f6, Func<T7?, Task<TResult?>>? f7)
        {
            if (Index == 7 && f7 != null)
                return f7(AsT7);
            else
                return MatchAsync(f0, f1, f2, f3, f4, f5, f6);
        }
        public void Switch(Action<T0?> a0, Action<T1?> a1, Action<T2?> a2, Action<T3?> a3, Action<T4?> a4, Action<T5?> a5, Action<T6?> a6, Action<T7?> a7)
        {
            if (Index == 7 && a7 != null)
                a7(AsT7);
            else
                Switch(a0, a1, a2, a3, a4, a5, a6);
        }
        public ValueTask SwitchAsync(Func<T0?, ValueTask> a0, Func<T1?, ValueTask> a1, Func<T2?, ValueTask> a2, Func<T3?, ValueTask> a3,
            Func<T4?, ValueTask> a4, Func<T5?, ValueTask> a5, Func<T6?, ValueTask> a6, Func<T7?, ValueTask> a7)
        {
            if (Index == 7 && a7 != null)
                return a7(AsT7);
            else
                return SwitchAsync(a0, a1, a2, a3, a4, a5, a6);
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
