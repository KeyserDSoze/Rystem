namespace RepositoryFramework
{
    public static class State
    {
        public static State<T, TKey> Ok<T, TKey>(T value, TKey key)
            where TKey : notnull
            => new(true, new Entity<T, TKey>(value, key));
        public static State<T, TKey> Ok<T, TKey>(Entity<T, TKey>? entity = default)
            where TKey : notnull
            => new(true, entity);
        public static State<T, TKey> NotOk<T, TKey>(T value, TKey key)
            where TKey : notnull
            => new(false, new Entity<T, TKey>(value, key));
        public static State<T, TKey> NotOk<T, TKey>(Entity<T, TKey>? entity = default)
         where TKey : notnull
         => new(false, entity);
        public static State<T, TKey> Default<T, TKey>(bool isOk, T value, TKey? key = default)
            where TKey : notnull
            => new(isOk, new Entity<T, TKey>(value, key));
        public static State<T, TKey> Default<T, TKey>(bool isOk, Entity<T, TKey>? entity = default)
         where TKey : notnull
         => new(isOk, entity);

        public static Task<State<T, TKey>> OkAsTask<T, TKey>(T value, TKey key)
           where TKey : notnull
           => Task.FromResult(new State<T, TKey>(true, new Entity<T, TKey>(value, key)));
        public static Task<State<T, TKey>> OkAsTask<T, TKey>(Entity<T, TKey>? entity = default)
            where TKey : notnull
            => Task.FromResult(new State<T, TKey>(true, entity));
        public static Task<State<T, TKey>> NotOkAsTask<T, TKey>(T value, TKey key)
            where TKey : notnull
            => Task.FromResult(new State<T, TKey>(false, new Entity<T, TKey>(value, key)));
        public static Task<State<T, TKey>> NotOkAsTask<T, TKey>(Entity<T, TKey>? entity = default)
         where TKey : notnull
         => Task.FromResult(new State<T, TKey>(false, entity));
        public static Task<State<T, TKey>> DefaultAsTask<T, TKey>(bool isOk, T value, TKey? key = default)
            where TKey : notnull
            => Task.FromResult(new State<T, TKey>(isOk, new Entity<T, TKey>(value, key)));
        public static Task<State<T, TKey>> DefaultAsTask<T, TKey>(bool isOk, Entity<T, TKey>? entity = default)
         where TKey : notnull
         => Task.FromResult(new State<T, TKey>(isOk, entity));
    }
}
