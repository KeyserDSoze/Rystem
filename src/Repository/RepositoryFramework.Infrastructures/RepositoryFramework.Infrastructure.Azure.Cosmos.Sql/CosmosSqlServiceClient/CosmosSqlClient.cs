using System.Reflection;
using Microsoft.Azure.Cosmos;

namespace RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
{
    public sealed class CosmosSqlClient
    {
        public Container Container { get; set; }
        public PropertyInfo[] Properties { get; set; }
    }
}
