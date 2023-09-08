using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace RepositoryFramework.Infrastructure.Dynamics.Dataverse
{
    internal sealed class DataverseClientWrapper<T, TKey> : IFactoryOptions
        where TKey : notnull
    {
        public ServiceClient Client { get; set; } = null!;
        public DataverseOptions<T, TKey> Settings { get; set; } = null!;
    }
}
