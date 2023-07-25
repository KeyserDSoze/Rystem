using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    public interface IRepositoryBaseBuilder<T, TKey, TRepositoryPattern, TRepositoryBuilder> : IRepositoryBaseBuilder<T, TKey, TRepositoryBuilder>
        where TKey : notnull
        where TRepositoryPattern : class
        where TRepositoryBuilder : IRepositoryBaseBuilder<T, TKey, TRepositoryPattern, TRepositoryBuilder>
    {
        TRepositoryBuilder SetStorageAndBuildOptions<TStorage, TStorageOptions, TConnection>(
            Action<TStorageOptions> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, TRepositoryPattern, IServiceWithOptions<TConnection>
            where TStorageOptions : class, IServiceOptions<TConnection>, new()
            where TConnection : class;
        Task<TRepositoryBuilder> SetStorageAndBuildOptionsAsync<TStorage, TStorageOptions, TConnection>(
            Action<TStorageOptions> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, TRepositoryPattern, IServiceWithOptions<TConnection>
            where TStorageOptions : class, IServiceOptionsAsync<TConnection>, new()
            where TConnection : class;
        TRepositoryBuilder SetStorage<TStorage>(string? name = null, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, TRepositoryPattern;
        TRepositoryBuilder SetStorageWithOptions<TStorage, TOptions>(Action<TOptions> options, string? name = null, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
           where TStorage : class, TRepositoryPattern, IServiceWithOptions<TOptions>
           where TOptions : class, new();
        Func<Task>? AfterBuildAsync { get; set; }
        Action? AfterBuild { get; set; }
    }
}
