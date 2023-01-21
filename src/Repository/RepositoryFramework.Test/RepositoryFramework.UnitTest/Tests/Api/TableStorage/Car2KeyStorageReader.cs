using System;
using RepositoryFramework.Infrastructure.Azure.Storage.Table;

namespace RepositoryFramework.Test.Models
{
    internal sealed class Car2KeyStorageReader : ITableStorageKeyReader<SuperCar, Guid>
    {
        public (string PartitionKey, string RowKey) Read(Guid key)
            => (key.ToString(), string.Empty);

        public Guid Read(SuperCar entity)
            => entity.Id;
    }
}
