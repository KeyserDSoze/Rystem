using System.Text.Json.Serialization;

namespace RepositoryFramework
{
    public static class Entity
    {
        public static Entity<T, TKey> Default<T, TKey>(T value, TKey key)
            where TKey : notnull
            => new(value, key);
    }
}
