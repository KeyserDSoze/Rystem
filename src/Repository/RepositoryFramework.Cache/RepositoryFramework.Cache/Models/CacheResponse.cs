namespace RepositoryFramework.Cache
{
    public sealed record CacheResponse<T>(bool IsPresent, T? Value);
}