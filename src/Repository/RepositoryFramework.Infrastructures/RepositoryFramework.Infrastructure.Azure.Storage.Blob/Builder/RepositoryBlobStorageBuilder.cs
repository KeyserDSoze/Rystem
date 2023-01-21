using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    internal sealed class RepositoryBlobStorageBuilder<T, TKey> : IRepositoryBlobStorageBuilder<T, TKey>
        where TKey : notnull
    {
        public RepositoryBlobStorageBuilder(IServiceCollection services)
            => Services = services;
        public IServiceCollection Services { get; }
        public IRepositoryBlobStorageBuilder<T, TKey> WithIndexing<TProperty>(
            Expression<Func<T, TProperty>> property)
        {
            var name = property.Body.ToString().Split('.').Last();
            var compiledProperty = property.Compile();
            BlobStorageSettings<T, TKey>.Instance.Paths.Add(new BlobStoragePathComposer<T>(x => compiledProperty.Invoke(x)?.ToString(), name));
            Services.AddSingleton(BlobStorageSettings<T, TKey>.Instance);
            return this;
        }
    }
}
