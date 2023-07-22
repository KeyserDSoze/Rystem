using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Storage.Blob
{
    public interface IRepositoryBlobStorageBuilder<T, TKey>
        where TKey : notnull
    {
        IServiceCollection Services { get; }
    }
}
