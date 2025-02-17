using System.Text.Json.Serialization;

namespace System
{
    [JsonConverter(typeof(UnionConverterFactory))]
    public class AnyOf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : AnyOf<T0, T1, T2, T3, T4, T5, T6, T7, T8>
    {
        public T9? AsT9 => TryGet<T9>(9);
        public T9 CastT9 => Get<T9>(9);
        public bool IsT9 => Index == 9;
        private protected override int NumberOfElements => 10;
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
            else if (Set<T9>(9, value))
                return true;
            return false;
        }
        public override Type? GetCurrentType()
        {
            var type = base.GetCurrentType();
            if (Index == 9)
                return typeof(T9);
            return type;
        }
        public TResult? Match<TResult>(Func<T0?, TResult>? f0, Func<T1?, TResult>? f1, Func<T2?, TResult>? f2, Func<T3?, TResult>? f3,
        Func<T4?, TResult>? f4, Func<T5?, TResult>? f5, Func<T6?, TResult>? f6, Func<T7?, TResult>? f7, Func<T8?, TResult>? f8,
        Func<T9?, TResult>? f9)
        {
            if (Index == 9 && f9 != null)
                return f9(AsT9);
            else
                return Match(f0, f1, f2, f3, f4, f5, f6, f7, f8);
        }
        public Task<TResult?> MatchAsync<TResult>(Func<T0?, Task<TResult?>>? f0, Func<T1?, Task<TResult?>>? f1, Func<T2?, Task<TResult?>>? f2, Func<T3?, Task<TResult?>>? f3,
            Func<T4?, Task<TResult?>>? f4, Func<T5?, Task<TResult?>>? f5, Func<T6?, Task<TResult?>>? f6, Func<T7?, Task<TResult?>>? f7, Func<T8?, Task<TResult?>>? f8,
            Func<T9?, Task<TResult?>>? f9)
        {
            if (Index == 9 && f9 != null)
                return f9(AsT9);
            else
                return MatchAsync(f0, f1, f2, f3, f4, f5, f6, f7, f8);
        }
        public void Switch(Action<T0?> a0, Action<T1?> a1, Action<T2?> a2, Action<T3?> a3, Action<T4?> a4,
            Action<T5?> a5, Action<T6?> a6, Action<T7?> a7, Action<T8?> a8, Action<T9?> a9)
        {
            if (Index == 9 && a9 != null)
                a9(AsT9);
            else
                Switch(a0, a1, a2, a3, a4, a5, a6, a7, a8);
        }
        public ValueTask SwitchAsync(Func<T0?, ValueTask> a0, Func<T1?, ValueTask> a1, Func<T2?, ValueTask> a2, Func<T3?, ValueTask> a3,
            Func<T4?, ValueTask> a4, Func<T5?, ValueTask> a5, Func<T6?, ValueTask> a6, Func<T7?, ValueTask> a7, Func<T8?, ValueTask> a8,
            Func<T9?, ValueTask> a9)
        {
            if (Index == 9 && a9 != null)
                return a9(AsT9);
            else
                return SwitchAsync(a0, a1, a2, a3, a4, a5, a6, a7, a8);
        }
        public bool TryGetT9(out T9? entity)
           => TryGet(9, out entity);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T0 entity) => new(entity, 0);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 entity) => new(entity, 1);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T2 entity) => new(entity, 2);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T3 entity) => new(entity, 3);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T4 entity) => new(entity, 4);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T5 entity) => new(entity, 5);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T6 entity) => new(entity, 6);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T7 entity) => new(entity, 7);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T8 entity) => new(entity, 8);
        public static implicit operator AnyOf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T9 entity) => new(entity, 9);
    }
}
