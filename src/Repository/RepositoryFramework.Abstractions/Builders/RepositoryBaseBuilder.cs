using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RepositoryFramework
{
    public abstract class RepositoryBaseBuilder<T, TKey, TRepository, TRepositoryConcretization, TRepositoryPattern, TRepositoryBuilder> : IRepositoryBaseBuilder<T, TKey, TRepositoryPattern, TRepositoryBuilder>
        where TKey : notnull
        where TRepositoryPattern : class
        where TRepositoryBuilder : IRepositoryBaseBuilder<T, TKey, TRepositoryPattern, TRepositoryBuilder>
        where TRepository : class
        where TRepositoryConcretization : class, TRepository
    {
        public IServiceCollection Services { get; internal set; }
        private TRepositoryBuilder Builder => (TRepositoryBuilder)(dynamic)this;
        private string _currentName = string.Empty;
        private ServiceLifetime _serviceLifetime = ServiceLifetime.Scoped;
        private void SetDefaultFrameworkBeforeStorage<TStorage>(
            string? name,
            ServiceLifetime serviceLifetime)
            where TStorage : class, TRepositoryPattern
        {
            var patternType = PatternType.Repository;
            if (!typeof(TRepository).IsSubclassOf(typeof(IRepositoryPattern)))
                patternType = typeof(TRepository).IsSubclassOf(typeof(ICommandPattern)) ? PatternType.Command : PatternType.Query;
            _currentName = name ?? string.Empty;
            _serviceLifetime = serviceLifetime;
            var service = SetService(_currentName, patternType);
            service.ServiceLifetime = serviceLifetime;
            service.InterfaceType = typeof(TRepository);
            service.ImplementationType = typeof(TStorage);
            Services.TryAddSingleton(KeySettings<TKey>.Instance);
            Services.AddFactory<TRepository, TRepositoryConcretization>(_currentName, serviceLifetime);
        }
        public async Task<TRepositoryBuilder> SetStorageAndBuildOptionsAsync<TStorage, TStorageOptions, TConnection>(
            Action<TStorageOptions> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, TRepositoryPattern, IServiceWithOptions<TConnection>
            where TStorageOptions : class, IServiceOptionsAsync<TConnection>, new()
            where TConnection : class
        {
            SetDefaultFrameworkBeforeStorage<TStorage>(name, serviceLifetime);
            await Services
                .AddFactoryAsync<TRepositoryPattern, TStorage, TStorageOptions, TConnection>(options, _currentName, serviceLifetime)
                .NoContext();
            return Builder;
        }
        public TRepositoryBuilder SetStorageAndBuildOptions<TStorage, TStorageOptions, TConnection>(
            Action<TStorageOptions> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, TRepositoryPattern, IServiceWithOptions<TConnection>
            where TStorageOptions : class, IServiceOptions<TConnection>, new()
            where TConnection : class
        {
            SetDefaultFrameworkBeforeStorage<TStorage>(name, serviceLifetime);
            Services
                .AddFactory<TRepositoryPattern, TStorage, TStorageOptions, TConnection>(options, _currentName, serviceLifetime);
            return Builder;
        }
        public TRepositoryBuilder SetStorageWithOptions<TStorage, TStorageOptions>(
            Action<TStorageOptions> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, TRepositoryPattern, IServiceWithOptions<TStorageOptions>
            where TStorageOptions : class, new()
        {
            SetDefaultFrameworkBeforeStorage<TStorage>(name, serviceLifetime);
            Services
                .AddFactory<TRepositoryPattern, TStorage, TStorageOptions>(options, _currentName, serviceLifetime);
            return Builder;
        }
        public TRepositoryBuilder SetStorage<TStorage>(
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, TRepositoryPattern
        {
            SetDefaultFrameworkBeforeStorage<TStorage>(name, serviceLifetime);
            Services
                .AddFactory<TRepositoryPattern, TStorage>(_currentName, serviceLifetime);
            return Builder;
        }
        private RepositoryFrameworkService SetService(string name, PatternType patternType)
        {
            var entityType = typeof(T);
            var serviceKey = RepositoryFrameworkRegistry.ToServiceKey(entityType, patternType, name);
            if (!RepositoryFrameworkRegistry.Instance.Services.ContainsKey(serviceKey))
            {
                var keyType = typeof(TKey);
                RepositoryFrameworkRegistry.Instance.Services.Add(serviceKey,
                    new(keyType, entityType, patternType, name));
                Services.TryAddSingleton(RepositoryFrameworkRegistry.Instance);
            }
            return RepositoryFrameworkRegistry.Instance.Services[serviceKey];
        }
        public RepositoryBusinessBuilder<T, TKey> AddBusiness(ServiceLifetime? serviceLifetime = null)
            => new(Services, serviceLifetime ?? _serviceLifetime);
        public QueryTranslationBuilder<T, TKey, TTranslated, TRepositoryBuilder> Translate<TTranslated>()
        {
            Services.AddSingleton<IRepositoryFilterTranslator<T, TKey>>(FilterTranslation<T, TKey>.Instance);
            FilterTranslation<T, TKey>.Instance.Setup<TTranslated>();
            Services.AddSingleton<IRepositoryMapper<T, TKey, TTranslated>>(RepositoryMapper<T, TKey, TTranslated>.Instance);
            return new QueryTranslationBuilder<T, TKey, TTranslated, TRepositoryBuilder>(Builder);
        }
    }
}
