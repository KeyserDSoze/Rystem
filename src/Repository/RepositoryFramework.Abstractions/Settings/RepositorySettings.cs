using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RepositoryFramework
{
    public sealed class RepositorySettings<T, TKey>
        where TKey : notnull
    {
        public IServiceCollection Services { get; }
        public PatternType Type { get; }
        public ServiceLifetime ServiceLifetime { get; private set; }
        public void SetNotExposable()
        {
            var service = SetService();
            service.IsNotExposable = true;
        }
        public RepositorySettings(IServiceCollection services, PatternType type)
        {
            Services = services;
            Type = type;
        }
        public IRepositoryBuilder<T, TKey, TStorage> SetStorage<TStorage>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class
            => Type switch
            {
                PatternType.Command => SetCommandStorage<TStorage>(serviceLifetime),
                PatternType.Query => SetQueryStorage<TStorage>(serviceLifetime),
                _ => SetRepositoryStorage<TStorage>(serviceLifetime)
            };
        private IRepositoryBuilder<T, TKey, TStorage> SetRepositoryStorage<TStorage>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class
            => SetStorage<TStorage, IRepository<T, TKey>, IRepositoryPattern<T, TKey>, Repository<T, TKey>>(serviceLifetime);
        private IRepositoryBuilder<T, TKey, TStorage> SetCommandStorage<TStorage>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class
            => SetStorage<TStorage, ICommand<T, TKey>, ICommandPattern<T, TKey>, Command<T, TKey>>(serviceLifetime);
        private IRepositoryBuilder<T, TKey, TStorage> SetQueryStorage<TStorage>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TStorage : class
            => SetStorage<TStorage, IQuery<T, TKey>, IQueryPattern<T, TKey>, Query<T, TKey>>(serviceLifetime);
        private IRepositoryBuilder<T, TKey, TStorage> SetStorage<TStorage, TRepository, TRepositoryPattern, TRepositoryConcretization>(ServiceLifetime serviceLifetime)
            where TStorage : class
            where TRepository : class
            where TRepositoryConcretization : class, TRepository
        {
            var storageType = typeof(TStorage);
            var currentType = typeof(TRepository);
            if (!storageType.GetInterfaces().Any(x => x == currentType))
            {
                throw new ArgumentException($"{storageType.FullName} is not a {currentType.FullName}");
            }
            var service = SetService();
            ServiceLifetime = serviceLifetime;
            service.ServiceLifetime = ServiceLifetime;
            service.InterfaceType = currentType;
            service.ImplementationType = typeof(TStorage);
            Services
                .RemoveServiceIfAlreadyInstalled<TStorage>(currentType, typeof(TRepositoryPattern))
                .RemoveServiceIfAlreadyInstalled<TStorage>(typeof(KeySettings<TKey>))
                .AddSingleton<KeySettings<TKey>>()
                .AddService(typeof(TRepositoryPattern), storageType, serviceLifetime)
                .AddService<TRepository, TRepositoryConcretization>(serviceLifetime);
            return new RepositoryBuilder<T, TKey, TStorage>(Services, PatternType.Query, serviceLifetime);
        }
        private RepositoryFrameworkService SetService()
        {
            var entityType = typeof(T);
            var serviceKey = RepositoryFrameworkRegistry.ToServiceKey(entityType, Type);
            if (!RepositoryFrameworkRegistry.Instance.Services.ContainsKey(serviceKey))
            {
                var keyType = typeof(TKey);
                RepositoryFrameworkRegistry.Instance.Services.Add(serviceKey,
                    new(keyType, entityType, Type));
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
}
