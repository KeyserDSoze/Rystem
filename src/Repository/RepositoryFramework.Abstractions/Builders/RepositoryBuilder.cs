using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    internal sealed class RepositoryBuilder<T, TKey, TStorage> : IRepositoryBuilder<T, TKey, TStorage>
        where TKey : notnull
        where TStorage : class
    {
        public IServiceCollection Services { get; }
        public PatternType Type { get; }
        public ServiceLifetime ServiceLifetime { get; }
        public RepositoryBuilder(IServiceCollection services, PatternType type, ServiceLifetime serviceLifetime)
        {
            Services = services;
            Type = type;
            ServiceLifetime = serviceLifetime;
        }
    }
}
