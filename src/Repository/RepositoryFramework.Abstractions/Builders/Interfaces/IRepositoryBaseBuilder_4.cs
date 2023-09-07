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
            where TStorage : class, TRepositoryPattern, IServiceWithFactoryWithOptions<TConnection>
            where TStorageOptions : class, IOptionsBuilder<TConnection>, new()
            where TConnection : class;
        TRepositoryBuilder SetStorageWithOptions<TStorage, TStorageOptions>(Action<TStorageOptions> options, string? name = null, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
           where TStorage : class, TRepositoryPattern, IServiceWithFactoryWithOptions<TStorageOptions>
           where TStorageOptions : class, new();
        Task<TRepositoryBuilder> SetStorageAndBuildOptionsAsync<TStorage, TStorageOptions, TConnection>(
            Action<TStorageOptions> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, TRepositoryPattern, IServiceWithFactoryWithOptions<TConnection>
            where TStorageOptions : class, IOptionsBuilderAsync<TConnection>, new()
            where TConnection : class;
        TRepositoryBuilder SetStorage<TStorage>(string? name = null, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, TRepositoryPattern;
        Func<Task>? AfterBuildAsync { get; set; }
        Action? AfterBuild { get; set; }
    }
}
