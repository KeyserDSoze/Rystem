using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RepositoryFramework.Abstractions;

namespace RepositoryFramework
{
    public abstract class RepositoryBaseBuilder<T, TKey, TRepository, TRepositoryConcretization, TRepositoryPattern, TRepositoryBuilder> : IRepositoryBaseBuilder<T, TKey, TRepositoryPattern, TRepositoryBuilder>
        where TKey : notnull
        where TRepository : class
        where TRepositoryBuilder : IRepositoryBaseBuilder<T, TKey, TRepositoryPattern, TRepositoryBuilder>
        where TRepositoryPattern : class
        where TRepositoryConcretization : class, TRepository
    {
        public IServiceCollection Services { get; }
        private TRepositoryBuilder Builder => (TRepositoryBuilder)(object)this;
        private string _currentName = string.Empty;
        private PatternType _currentPatternType = PatternType.Repository;
        private ServiceLifetime _serviceLifetime = ServiceLifetime.Scoped;
        private protected RepositoryBaseBuilder(IServiceCollection services)
        {
            Services = services;
        }
        public Func<Task>? AfterBuildAsync { get; set; }
        public Action? AfterBuild { get; set; }
        private void SetDefaultFrameworkBeforeStorage<TStorage>(
            string? name,
            ServiceLifetime serviceLifetime)
            where TStorage : class, TRepositoryPattern
        {
            if (typeof(TRepository).GetInterface(nameof(IRepositoryPattern)) == null)
                _currentPatternType = typeof(TRepository).GetInterface(nameof(ICommandPattern)) != null
                    ? PatternType.Command : PatternType.Query;
            _currentName = name ?? string.Empty;
            _serviceLifetime = serviceLifetime;
            var service = SetService();
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
            where TStorage : class, TRepositoryPattern, IServiceWithFactoryWithOptions<TConnection>
            where TStorageOptions : class, IOptionsBuilderAsync<TConnection>, new()
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
            where TStorage : class, TRepositoryPattern, IServiceWithFactoryWithOptions<TConnection>
            where TStorageOptions : class, IOptionsBuilder<TConnection>, new()
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
            where TStorage : class, TRepositoryPattern, IServiceWithFactoryWithOptions<TStorageOptions>
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
        public RepositoryFrameworkService SetService()
        {
            var entityType = typeof(T);
            var serviceKey = RepositoryFrameworkRegistry.ToServiceKey(entityType, _currentPatternType, _currentName);
            var registry = Services.TryAddSingletonAndGetService<RepositoryFrameworkRegistry>();
            if (!registry.Services.ContainsKey(serviceKey))
            {
                var keyType = typeof(TKey);
                registry.Services.Add(serviceKey,
                    new(keyType, entityType, _currentPatternType, _currentName));
            }
            return registry.Services[serviceKey];
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
        public void SetNotExposable()
        {
            var service = SetService();
            service.ExposedMethods = RepositoryMethods.None;
        }
        public void SetExposable(RepositoryMethods methods = RepositoryMethods.All)
        {
            var service = SetService();
            service.ExposedMethods = methods;
        }
        public void SetOnlyQueryExposable()
        {
            var service = SetService();
            service.ExposedMethods = RepositoryMethods.Exist | RepositoryMethods.Get | RepositoryMethods.Query | RepositoryMethods.Operation;
        }
        public void SetOnlyCommandExposable()
        {
            var service = SetService();
            service.ExposedMethods = RepositoryMethods.Insert | RepositoryMethods.Update | RepositoryMethods.Delete | RepositoryMethods.Batch;
        }
        public void SetExamples(T entity, TKey key)
        {
            Services
                .AddOrOverrideSingleton<IRepositoryExamples<T, TKey>>(
                    new RepositoryExamples<T, TKey>(entity, key));
        }
    }
}
