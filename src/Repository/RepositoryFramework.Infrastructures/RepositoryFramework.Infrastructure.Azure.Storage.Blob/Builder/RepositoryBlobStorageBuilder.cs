using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    internal sealed class RepositoryBlobStorageBuilder<T, TKey> : IRepositoryBlobStorageBuilder<T, TKey>
        where TKey : notnull
    {
        public RepositoryBlobStorageBuilder(IServiceCollection services)
            => Services = services;
        public IServiceCollection Services { get; }
    }
}
