using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    public sealed class CosmosSqlClient : IFactoryOptions
    {
        public Container Container { get; set; } = null!;
        public PropertyInfo[] Properties { get; set; } = null!;
        public string ExistsQuery { get; set; } = null!;
    }
}
