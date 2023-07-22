using Microsoft.PowerPlatform.Dataverse.Client;

namespace RepositoryFramework.Infrastructure.Dynamics.Dataverse
{
    public sealed class DataverseClientWrapper
    {
        public ServiceClient Client { get; set; } = null!;
    }
}
