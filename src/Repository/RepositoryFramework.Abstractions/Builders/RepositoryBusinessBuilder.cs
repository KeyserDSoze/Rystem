using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    public sealed class RepositoryBusinessBuilder<T, TKey>
        where TKey : notnull
    {
        public IServiceCollection Services { get; }
        public ServiceLifetime ServiceLifetime { get; }
        public RepositoryBusinessBuilder(IServiceCollection services, ServiceLifetime? serviceLifetime)
        {
            Services = services;
            if (serviceLifetime != null)
                ServiceLifetime = serviceLifetime.Value;
            else
            {
                var entityType = typeof(T);
                var registry = Services.TryAddSingletonAndGetService<RepositoryFrameworkRegistry>();
                var servicesByModel = registry.GetByModel(entityType);
                ServiceLifetime = servicesByModel.OrderByDescending(x => x.ServiceLifetime).FirstOrDefault()?.ServiceLifetime ?? ServiceLifetime.Transient;
            }
        }
        public RepositoryBusinessBuilder<T, TKey> AddBusinessBeforeInsert<TBusiness>()
           where TBusiness : class, IRepositoryBusinessBeforeInsert<T, TKey>
           => AddBusiness<IRepositoryBusinessBeforeInsert<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessAfterInsert<TBusiness>()
            where TBusiness : class, IRepositoryBusinessAfterInsert<T, TKey>
            => AddBusiness<IRepositoryBusinessAfterInsert<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessBeforeUpdate<TBusiness>()
            where TBusiness : class, IRepositoryBusinessBeforeUpdate<T, TKey>
            => AddBusiness<IRepositoryBusinessBeforeUpdate<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessAfterUpdate<TBusiness>()
            where TBusiness : class, IRepositoryBusinessAfterUpdate<T, TKey>
            => AddBusiness<IRepositoryBusinessAfterUpdate<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessBeforeDelete<TBusiness>()
            where TBusiness : class, IRepositoryBusinessBeforeDelete<T, TKey>
            => AddBusiness<IRepositoryBusinessBeforeDelete<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessAfterDelete<TBusiness>()
            where TBusiness : class, IRepositoryBusinessAfterDelete<T, TKey>
            => AddBusiness<IRepositoryBusinessAfterDelete<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessBeforeBatch<TBusiness>()
            where TBusiness : class, IRepositoryBusinessBeforeBatch<T, TKey>
            => AddBusiness<IRepositoryBusinessBeforeBatch<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessAfterBatch<TBusiness>()
            where TBusiness : class, IRepositoryBusinessAfterBatch<T, TKey>
            => AddBusiness<IRepositoryBusinessAfterBatch<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessBeforeGet<TBusiness>()
            where TBusiness : class, IRepositoryBusinessBeforeGet<T, TKey>
            => AddBusiness<IRepositoryBusinessBeforeGet<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessAfterGet<TBusiness>()
            where TBusiness : class, IRepositoryBusinessAfterGet<T, TKey>
            => AddBusiness<IRepositoryBusinessAfterGet<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessBeforeExist<TBusiness>()
           where TBusiness : class, IRepositoryBusinessBeforeExist<T, TKey>
           => AddBusiness<IRepositoryBusinessBeforeExist<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessAfterExist<TBusiness>()
            where TBusiness : class, IRepositoryBusinessAfterExist<T, TKey>
            => AddBusiness<IRepositoryBusinessAfterExist<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessBeforeQuery<TBusiness>()
           where TBusiness : class, IRepositoryBusinessBeforeQuery<T, TKey>
           => AddBusiness<IRepositoryBusinessBeforeQuery<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessAfterQuery<TBusiness>()
            where TBusiness : class, IRepositoryBusinessAfterQuery<T, TKey>
            => AddBusiness<IRepositoryBusinessAfterQuery<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessBeforeOperation<TBusiness>()
           where TBusiness : class, IRepositoryBusinessBeforeOperation<T, TKey>
           => AddBusiness<IRepositoryBusinessBeforeOperation<T, TKey>, TBusiness>();
        public RepositoryBusinessBuilder<T, TKey> AddBusinessAfterOperation<TBusiness>()
            where TBusiness : class, IRepositoryBusinessAfterOperation<T, TKey>
            => AddBusiness<IRepositoryBusinessAfterOperation<T, TKey>, TBusiness>();
        private RepositoryBusinessBuilder<T, TKey> AddBusiness<TBusinessInterface, TBusiness>()
            where TBusinessInterface : class
            where TBusiness : class, TBusinessInterface
        {
            Services
                .AddService<TBusinessInterface, TBusiness>(ServiceLifetime)
                .TryAddService<IRepositoryBusinessManager<T, TKey>, RepositoryBusinessManager<T, TKey>>(ServiceLifetime);
            return this;
        }
    }
}
