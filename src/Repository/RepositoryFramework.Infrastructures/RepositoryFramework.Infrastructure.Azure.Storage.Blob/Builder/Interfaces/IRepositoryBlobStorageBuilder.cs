using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    public interface IRepositoryBlobStorageBuilder<T, TKey>
        where TKey : notnull
    {
        IRepositoryBlobStorageBuilder<T, TKey> WithIndexing<TProperty>(
           Expression<Func<T, TProperty>> property);
        IServiceCollection Services { get; }
    }
}
