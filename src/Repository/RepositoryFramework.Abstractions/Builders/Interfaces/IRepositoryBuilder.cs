using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    /// <summary>
    /// Builder for your repository framewrk
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    /// <typeparam name="TStorage">Storage for your repository.</typeparam>
    public interface IRepositoryBuilder<T, TKey, out TStorage>
        where TKey : notnull
        where TStorage : class
    {
        IServiceCollection Services { get; }
        PatternType Type { get; }
        ServiceLifetime ServiceLifetime { get; }
    }
}
