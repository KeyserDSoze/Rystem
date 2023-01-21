using RepositoryFramework.Infrastructure.Azure.Storage.Table;
using RepositoryFramework.Test.Domain;

namespace RepositoryFramework.UnitTest.Tests.AllIntegration.TableStorage
{
    internal class TableStorageKeyReader : ITableStorageKeyReader<AppUser, AppUserKey>
    {
        public (string PartitionKey, string RowKey) Read(AppUserKey key)
            => (key.Id.ToString(), string.Empty);

        public AppUserKey Read(AppUser entity)
            => new(entity.Id);
    }
}
