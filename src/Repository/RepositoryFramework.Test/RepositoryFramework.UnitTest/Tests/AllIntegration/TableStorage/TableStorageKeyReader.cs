using RepositoryFramework.Infrastructure.Azure.Storage.Table;
using RepositoryFramework.Test.Domain;

namespace RepositoryFramework.UnitTest.Tests.AllIntegration.TableStorage
{
    internal class TableStorageKeyReader : ITableStorageKeyReader<AppUser, AppUserKey>
    {
        public (string PartitionKey, string RowKey) Read(AppUserKey key, TableStorageSettings<AppUser, AppUserKey> settings)
            => (key.Id.ToString(), string.Empty);

        public AppUserKey Read(AppUser entity, TableStorageSettings<AppUser, AppUserKey> settings)
            => new(entity.Id);
    }
}
