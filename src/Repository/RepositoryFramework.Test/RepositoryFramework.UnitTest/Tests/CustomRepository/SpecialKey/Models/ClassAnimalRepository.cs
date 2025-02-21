using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.UnitTest.CustomRepository.SpecialKeys.Models
{
    public class ClassAnimalRepository : IRepository<ClassAnimal, ClassAnimalKey>
    {
        private static readonly Dictionary<string, Dictionary<int, Dictionary<Guid, ClassAnimal>>> s_dic = [];
        public IAsyncEnumerable<BatchResult<ClassAnimal, ClassAnimalKey>> BatchAsync(BatchOperations<ClassAnimal, ClassAnimalKey> operations, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<TProperty> OperationAsync<TProperty>(
         OperationType<TProperty> operation,
         IFilterExpression filter,
         CancellationToken cancellationToken = default)
        {
            if (operation.Name == DefaultOperations.Count)
                return ValueTask.FromResult((TProperty)(object)s_dic.Count);
            else
                throw new NotImplementedException();
        }

        public async Task<State<ClassAnimal, ClassAnimalKey>> DeleteAsync(ClassAnimalKey key, CancellationToken cancellationToken = default)
        {
            await Task.Delay(0, cancellationToken).NoContext();
            if (s_dic.TryGetValue(key.Id, out var value) && value.TryGetValue(key.Key, out var innerValue) && innerValue.ContainsKey(key.ValKey))
            {
                innerValue.Remove(key.ValKey);
                return true;
            }
            return false;
        }

        public async Task<State<ClassAnimal, ClassAnimalKey>> ExistAsync(ClassAnimalKey key, CancellationToken cancellationToken = default)
        {
            await Task.Delay(0, cancellationToken).NoContext();
            return s_dic.ContainsKey(key.Id) && s_dic[key.Id].ContainsKey(key.Key) && s_dic[key.Id][key.Key].ContainsKey(key.ValKey);
        }

        public async Task<ClassAnimal?> GetAsync(ClassAnimalKey key, CancellationToken cancellationToken = default)
        {
            await Task.Delay(0, cancellationToken).NoContext();
            if (s_dic.TryGetValue(key.Id, out var value) && value.TryGetValue(key.Key, out var innerValue) && innerValue.TryGetValue(key.ValKey, out var lastValue))
            {
                return lastValue;
            }
            return default;
        }

        public async Task<State<ClassAnimal, ClassAnimalKey>> InsertAsync(ClassAnimalKey key, ClassAnimal value, CancellationToken cancellationToken = default)
        {
            await Task.Delay(0, cancellationToken).NoContext();
            if (!s_dic.ContainsKey(key.Id))
                s_dic.Add(key.Id, []);
            if (!s_dic[key.Id].ContainsKey(key.Key))
                s_dic[key.Id].Add(key.Key, []);
            if (s_dic[key.Id][key.Key].TryAdd(key.ValKey, value))
            {
                return true;
            }
            return false;
        }

        public async Task<State<ClassAnimal, ClassAnimalKey>> UpdateAsync(ClassAnimalKey key, ClassAnimal animalValue, CancellationToken cancellationToken = default)
        {
            await Task.Delay(0, cancellationToken).NoContext();
            if (s_dic.TryGetValue(key.Id, out var value) && value.TryGetValue(key.Key, out var innerValue) && innerValue.ContainsKey(key.ValKey))
            {
                innerValue[key.ValKey] = animalValue;
                return true;
            }
            return false;
        }

        public IAsyncEnumerable<Entity<ClassAnimal, ClassAnimalKey>> QueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<bool> BootstrapAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
