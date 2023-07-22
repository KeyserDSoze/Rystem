using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RepositoryFramework
{
    //todo it needs to accept if command only command storage, we need to do a common interface
    public partial class RepositorySettings<T, TKey>
       where TKey : notnull
    {
        public Task<IRepositoryBuilder<T, TKey, TStorage>> SetStorage<TStorage, TStorageOptions, TConnection>(
            Action<TStorageOptions> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class
            where TStorageOptions : class, IServiceOptions<TConnection>, new()
            where TConnection : class
            => Generics
                .With(typeof(RepositorySettings<T, TKey>),
                 $"Set{Type}StorageSync", typeof(TStorage), typeof(TStorageOptions), typeof(TConnection))
                .InvokeAsync<IRepositoryBuilder<T, TKey, TStorage>>(this, name ?? string.Empty, options, serviceLifetime);
        private Task<IRepositoryBuilder<T, TKey, TStorage>> SetRepositoryStorageSync<TStorage, TStorageOptions, TConnection>(
            string name,
            Action<TStorageOptions> options,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, IRepositoryPattern<T, TKey>, IServiceWithOptions<TConnection>
            where TStorageOptions : class, IServiceOptions<TConnection>, new()
            where TConnection : class
            => SetStorage<TStorage, IRepository<T, TKey>, IRepositoryPattern<T, TKey>, Repository<T, TKey>, TStorageOptions, TConnection>(name, options, serviceLifetime);
        private Task<IRepositoryBuilder<T, TKey, TStorage>> SetCommandStorageSync<TStorage, TStorageOptions, TConnection>(
            string name,
            Action<TStorageOptions> options,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, ICommandPattern<T, TKey>, IServiceWithOptions<TConnection>
            where TStorageOptions : class, IServiceOptions<TConnection>, new()
            where TConnection : class
            => SetStorage<TStorage, ICommand<T, TKey>, ICommandPattern<T, TKey>, Command<T, TKey>, TStorageOptions, TConnection>(name, options, serviceLifetime);
        private Task<IRepositoryBuilder<T, TKey, TStorage>> SetQueryStorageSync<TStorage, TStorageOptions, TConnection>(
            string name,
            Action<TStorageOptions> options,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, IQueryPattern<T, TKey>, IServiceWithOptions<TConnection>
            where TStorageOptions : class, IServiceOptions<TConnection>, new()
            where TConnection : class
            => SetStorage<TStorage, IQuery<T, TKey>, IQueryPattern<T, TKey>, Query<T, TKey>, TStorageOptions, TConnection>(name, options, serviceLifetime);
        private async Task<IRepositoryBuilder<T, TKey, TStorage>> SetStorage<TStorage, TRepository, TRepositoryPattern, TRepositoryConcretization, TStorageOptions, TConnection>(
            string name,
            Action<TStorageOptions> options,
            ServiceLifetime serviceLifetime)
            where TStorage : class, TRepositoryPattern, IServiceWithOptions<TConnection>
            where TRepositoryPattern : class
            where TRepository : class
            where TRepositoryConcretization : class, TRepository
            where TStorageOptions : class, IServiceOptions<TConnection>, new()
            where TConnection : class
        {
            var service = SetService(name);
            ServiceLifetime = serviceLifetime;
            service.ServiceLifetime = ServiceLifetime;
            service.InterfaceType = typeof(TRepository);
            service.ImplementationType = typeof(TStorage);
            Services.TryAddSingleton(KeySettings<TKey>.Instance);
            Services.AddFactory<TRepository, TRepositoryConcretization>(name, serviceLifetime);
            Services
                .AddFactory<TRepositoryPattern, TStorage, TStorageOptions, TConnection>(options, name, serviceLifetime);
            return new RepositoryBuilder<T, TKey, TStorage>(Services, PatternType.Query, serviceLifetime);
        }
    }
    public partial class RepositorySettings<T, TKey>
        where TKey : notnull
    {
        public Task<IRepositoryBuilder<T, TKey, TStorage>> SetStorageAsync<TStorage, TStorageOptions, TConnection>(
            Action<TStorageOptions> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class
            where TStorageOptions : class, IServiceOptionsAsync<TConnection>, new()
            where TConnection : class
            => Generics
                .With(typeof(RepositorySettings<T, TKey>),
                 $"Set{Type}StorageAsync", typeof(TStorage), typeof(TStorageOptions), typeof(TConnection))
                .InvokeAsync<IRepositoryBuilder<T, TKey, TStorage>>(this, name ?? string.Empty, options, serviceLifetime);
        private Task<IRepositoryBuilder<T, TKey, TStorage>> SetRepositoryStorageAsync<TStorage, TStorageOptions, TConnection>(
            string name,
            Action<TStorageOptions> options,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, IRepositoryPattern<T, TKey>, IServiceWithOptions<TConnection>
            where TStorageOptions : class, IServiceOptionsAsync<TConnection>, new()
            where TConnection : class
            => SetStorageAsync<TStorage, IRepository<T, TKey>, IRepositoryPattern<T, TKey>, Repository<T, TKey>, TStorageOptions, TConnection>(name, options, serviceLifetime);
        private Task<IRepositoryBuilder<T, TKey, TStorage>> SetCommandStorageAsync<TStorage, TStorageOptions, TConnection>(
            string name,
            Action<TStorageOptions> options,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, ICommandPattern<T, TKey>, IServiceWithOptions<TConnection>
            where TStorageOptions : class, IServiceOptionsAsync<TConnection>, new()
            where TConnection : class
            => SetStorageAsync<TStorage, ICommand<T, TKey>, ICommandPattern<T, TKey>, Command<T, TKey>, TStorageOptions, TConnection>(name, options, serviceLifetime);
        private Task<IRepositoryBuilder<T, TKey, TStorage>> SetQueryStorageAsync<TStorage, TStorageOptions, TConnection>(
            string name,
            Action<TStorageOptions> options,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, IQueryPattern<T, TKey>, IServiceWithOptions<TConnection>
            where TStorageOptions : class, IServiceOptionsAsync<TConnection>, new()
            where TConnection : class
            => SetStorageAsync<TStorage, IQuery<T, TKey>, IQueryPattern<T, TKey>, Query<T, TKey>, TStorageOptions, TConnection>(name, options, serviceLifetime);
        private async Task<IRepositoryBuilder<T, TKey, TStorage>> SetStorageAsync<TStorage, TRepository, TRepositoryPattern, TRepositoryConcretization, TStorageOptions, TConnection>(
            string name,
            Action<TStorageOptions> options,
            ServiceLifetime serviceLifetime)
            where TStorage : class, TRepositoryPattern, IServiceWithOptions<TConnection>
            where TRepositoryPattern : class
            where TRepository : class
            where TRepositoryConcretization : class, TRepository
            where TStorageOptions : class, IServiceOptionsAsync<TConnection>, new()
            where TConnection : class
        {
            var service = SetService(name);
            ServiceLifetime = serviceLifetime;
            service.ServiceLifetime = ServiceLifetime;
            service.InterfaceType = typeof(TRepository);
            service.ImplementationType = typeof(TStorage);
            Services.TryAddSingleton(KeySettings<TKey>.Instance);
            Services.AddFactory<TRepository, TRepositoryConcretization>(name, serviceLifetime);
            await Services
                .AddFactoryAsync<TRepositoryPattern, TStorage, TStorageOptions, TConnection>(options, name, serviceLifetime)
                .NoContext();
            return new RepositoryBuilder<T, TKey, TStorage>(Services, PatternType.Query, serviceLifetime);
        }
    }
    public partial class RepositorySettings<T, TKey>
            where TKey : notnull
    {
        public IServiceCollection Services { get; }
        public PatternType Type { get; }
        public ServiceLifetime ServiceLifetime { get; private set; }
        public void SetNotExposable(string? name = null)
        {
            var service = SetService(name ?? string.Empty);
            service.IsNotExposable = true;
        }
        public RepositorySettings(IServiceCollection services, PatternType? type = null)
        {
            Services = services;
            if (type != null)
                Type = type.Value;
            else
            {
                var entityType = typeof(T);
                var servicesByModel = RepositoryFrameworkRegistry.Instance.GetByModel(entityType);
                Type = servicesByModel.FirstOrDefault()?.Type ?? PatternType.Repository;
            }
        }
        public IRepositoryBuilder<T, TKey, TStorage> SetStorage<TStorage>(
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class
             => Generics
                .With(typeof(RepositorySettings<T, TKey>),
                 $"Set{Type}Storage", typeof(TStorage))
                .Invoke<IRepositoryBuilder<T, TKey, TStorage>>(this, name ?? string.Empty, serviceLifetime)!;
        private IRepositoryBuilder<T, TKey, TStorage> SetRepositoryStorage<TStorage>(string name, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, IRepositoryPattern<T, TKey>
            => SetStorage<TStorage, IRepository<T, TKey>, IRepositoryPattern<T, TKey>, Repository<T, TKey>>(name, serviceLifetime);
        private IRepositoryBuilder<T, TKey, TStorage> SetCommandStorage<TStorage>(string name, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, ICommandPattern<T, TKey>
            => SetStorage<TStorage, ICommand<T, TKey>, ICommandPattern<T, TKey>, Command<T, TKey>>(name, serviceLifetime);
        private IRepositoryBuilder<T, TKey, TStorage> SetQueryStorage<TStorage>(string name, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, IQueryPattern<T, TKey>
            => SetStorage<TStorage, IQuery<T, TKey>, IQueryPattern<T, TKey>, Query<T, TKey>>(name, serviceLifetime);
        private IRepositoryBuilder<T, TKey, TStorage> SetStorage<TStorage, TRepository, TRepositoryPattern, TRepositoryConcretization>(string name, ServiceLifetime serviceLifetime)
            where TStorage : class, TRepositoryPattern
            where TRepositoryPattern : class
            where TRepository : class
            where TRepositoryConcretization : class, TRepository
        {
            var service = SetService(name);
            ServiceLifetime = serviceLifetime;
            service.ServiceLifetime = ServiceLifetime;
            service.InterfaceType = typeof(TRepository);
            service.ImplementationType = typeof(TStorage);
            Services.TryAddSingleton(KeySettings<TKey>.Instance);
            Services.AddFactory<TRepository, TRepositoryConcretization>(name, serviceLifetime);
            Services
                .AddFactory<TRepositoryPattern, TStorage>(name, serviceLifetime);
            return new RepositoryBuilder<T, TKey, TStorage>(Services, PatternType.Query, serviceLifetime);
        }
        private RepositoryFrameworkService SetService(string name)
        {
            var entityType = typeof(T);
            var serviceKey = RepositoryFrameworkRegistry.ToServiceKey(entityType, Type, name);
            if (!RepositoryFrameworkRegistry.Instance.Services.ContainsKey(serviceKey))
            {
                var keyType = typeof(TKey);
                RepositoryFrameworkRegistry.Instance.Services.Add(serviceKey,
                    new(keyType, entityType, Type, name));
                Services.TryAddSingleton(RepositoryFrameworkRegistry.Instance);
            }
            return RepositoryFrameworkRegistry.Instance.Services[serviceKey];
        }
        /// <summary>
        /// Add business to your repository or CQRS pattern.
        /// </summary>
        /// <param name="serviceLifetime">Service Lifetime to override the actual lifetime of your repository.</param>
        /// <returns>RepositoryBusinessSettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public RepositoryBusinessSettings<T, TKey> AddBusiness(ServiceLifetime? serviceLifetime = null)
        {
            serviceLifetime ??= ServiceLifetime;
            return new(Services, serviceLifetime.Value);
        }
        public IQueryTranslationBuilder<T, TKey, TTranslated> Translate<TTranslated>()
        {
            Services.AddSingleton<IRepositoryFilterTranslator<T, TKey>>(FilterTranslation<T, TKey>.Instance);
            FilterTranslation<T, TKey>.Instance.Setup<TTranslated>();
            Services.AddSingleton<IRepositoryMapper<T, TKey, TTranslated>>(RepositoryMapper<T, TKey, TTranslated>.Instance);
            return new QueryTranslationBuilder<T, TKey, TTranslated>(this);
        }
    }
    public partial class RepositorySettings<T, TKey>
            where TKey : notnull
    {
        public IRepositoryBuilder<T, TKey, TStorage> SetStorageWithOptions<TStorage, TOptions>(
            Action<TOptions> options,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, IServiceWithOptions<TOptions>
            where TOptions : class, new()
             => Generics
                .With(typeof(RepositorySettings<T, TKey>),
                 $"Set{Type}StorageWithOptions", typeof(TStorage))
                .Invoke<IRepositoryBuilder<T, TKey, TStorage>>(this, name ?? string.Empty, options, serviceLifetime)!;
        private IRepositoryBuilder<T, TKey, TStorage> SetRepositoryStorageWithOptions<TStorage, TOptions>(string name, Action<TOptions> options, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, IRepositoryPattern<T, TKey>, IServiceWithOptions<TOptions>
             where TOptions : class, new()
            => SetStorageWithOptions<TStorage, IRepository<T, TKey>, IRepositoryPattern<T, TKey>, Repository<T, TKey>, TOptions>(name, options, serviceLifetime);
        private IRepositoryBuilder<T, TKey, TStorage> SetCommandStorageWithOptions<TStorage, TOptions>(string name, Action<TOptions> options, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, ICommandPattern<T, TKey>, IServiceWithOptions<TOptions>
             where TOptions : class, new()
            => SetStorageWithOptions<TStorage, ICommand<T, TKey>, ICommandPattern<T, TKey>, Command<T, TKey>, TOptions>(name, options, serviceLifetime);
        private IRepositoryBuilder<T, TKey, TStorage> SetQueryStoragWithOptionse<TStorage, TOptions>(
            string name,
            Action<TOptions> options,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class, IQueryPattern<T, TKey>, IServiceWithOptions<TOptions>
             where TOptions : class, new()
            => SetStorageWithOptions<TStorage, IQuery<T, TKey>, IQueryPattern<T, TKey>, Query<T, TKey>, TOptions>(name, options, serviceLifetime);
        private IRepositoryBuilder<T, TKey, TStorage> SetStorageWithOptions<TStorage, TRepository, TRepositoryPattern, TRepositoryConcretization, TOptions>(
            string name,
            Action<TOptions> options,
            ServiceLifetime serviceLifetime)
            where TStorage : class, TRepositoryPattern, IServiceWithOptions<TOptions>
             where TOptions : class, new()
            where TRepositoryPattern : class
            where TRepository : class
            where TRepositoryConcretization : class, TRepository
        {
            var service = SetService(name);
            ServiceLifetime = serviceLifetime;
            service.ServiceLifetime = ServiceLifetime;
            service.InterfaceType = typeof(TRepository);
            service.ImplementationType = typeof(TStorage);
            Services.TryAddSingleton(KeySettings<TKey>.Instance);
            Services.AddFactory<TRepository, TRepositoryConcretization>(name, serviceLifetime);
            Services
                .AddFactory<TRepositoryPattern, TStorage, TOptions>(options, name, serviceLifetime);
            return new RepositoryBuilder<T, TKey, TStorage>(Services, PatternType.Query, serviceLifetime);
        }
    }
}
