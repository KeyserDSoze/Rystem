﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RepositoryFramework.UnitTest.QueryWithDifferentModelsAmongRepositoryAndStorage.Models;
using Xunit;

namespace RepositoryFramework.UnitTest.QueryWithDifferentModelsAmongRepositoryAndStorage.Storage
{
    public class CarRepository : IRepository<Car, int>
    {
        private readonly List<Auto> _database = new()
        {
            new Auto { Identificativo = 5, Identificativo2 = 5, Targa = "03djkd0", NumeroRuote = 2, Guidatore = new() },
            new Auto { Identificativo = 1, Identificativo2 = 1, Targa = "03djks0", NumeroRuote = 4, Guidatore = new() },
            new Auto { Identificativo = 2, Identificativo2 = 2, Targa = "03djka0", NumeroRuote = 4, Guidatore = new() },
            new Auto { Identificativo = 3, Identificativo2 = 3, Targa = "03djkb0", NumeroRuote = 3, Guidatore = new() },
            new Auto { Identificativo = 4, Identificativo2 = 4, Targa = "03djkc0", NumeroRuote = 2, Guidatore = new() },
        };
        public IAsyncEnumerable<BatchResult<Car, int>> BatchAsync(BatchOperations<Car, int> operations, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<bool> BootstrapAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<State<Car, int>> DeleteAsync(int key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<State<Car, int>> ExistAsync(int key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Car?> GetAsync(int key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<State<Car, int>> InsertAsync(int key, Car value, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<TProperty> OperationAsync<TProperty>(
          OperationType<TProperty> operation,
          IFilterExpression filter,
          CancellationToken cancellationToken = default)
        {
            if (operation.Name == DefaultOperations.Count)
                return ValueTask.FromResult((TProperty)Convert.ChangeType(_database.Count, typeof(TProperty)));
            else
                throw new NotImplementedException();
        }
        public async IAsyncEnumerable<Entity<Car, int>> QueryAsync(IFilterExpression filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(0, cancellationToken);
            var filtered = filter.Apply(_database).ToList();
            var filters = filter.GetFilters();
            if (filters.Count > 0)
                Assert.True(filters.SelectMany(x => x.Map).Select(x => x.Value).Where(x => x.Value != null).Count() > 0);
            foreach (var item in filtered?.Select(x => new Car { Id = x.Identificativo, Plate = x.Targa, NumberOfWheels = x.NumeroRuote }) ?? new List<Car>())
                yield return Entity.Default(item, item.Id);
        }

        public Task<State<Car, int>> UpdateAsync(int key, Car value, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
