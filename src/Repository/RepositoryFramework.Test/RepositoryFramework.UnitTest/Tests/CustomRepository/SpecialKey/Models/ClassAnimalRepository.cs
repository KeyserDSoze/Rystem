using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace RepositoryFramework.UnitTest.CustomRepository.SpecialKeys.Models
{
    public class ClassAnimalRepository : IRepository<ClassAnimal, ClassAnimalKey>
    {
        private static readonly Dictionary<string, Dictionary<int, Dictionary<Guid, ClassAnimal>>> s_dic = new();
        public Task<BatchResults<ClassAnimal, ClassAnimalKey>> BatchAsync(BatchOperations<ClassAnimal, ClassAnimalKey> operations, CancellationToken cancellationToken = default)
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
            if (s_dic.ContainsKey(key.Id) && s_dic[key.Id].ContainsKey(key.Key) && s_dic[key.Id][key.Key].ContainsKey(key.ValKey))
            {
                s_dic[key.Id][key.Key].Remove(key.ValKey);
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
            if (s_dic.ContainsKey(key.Id) && s_dic[key.Id].ContainsKey(key.Key) && s_dic[key.Id][key.Key].ContainsKey(key.ValKey))
            {
                return s_dic[key.Id][key.Key][key.ValKey];
            }
            return default;
        }

        public async Task<State<ClassAnimal, ClassAnimalKey>> InsertAsync(ClassAnimalKey key, ClassAnimal value, CancellationToken cancellationToken = default)
        {
            await Task.Delay(0, cancellationToken).NoContext();
            if (!s_dic.ContainsKey(key.Id))
                s_dic.Add(key.Id, new());
            if (!s_dic[key.Id].ContainsKey(key.Key))
                s_dic[key.Id].Add(key.Key, new());
            if (!s_dic[key.Id][key.Key].ContainsKey(key.ValKey))
            {
                s_dic[key.Id][key.Key].Add(key.ValKey, value);
                return true;
            }
            return false;
        }

        public async Task<State<ClassAnimal, ClassAnimalKey>> UpdateAsync(ClassAnimalKey key, ClassAnimal value, CancellationToken cancellationToken = default)
        {
            await Task.Delay(0, cancellationToken).NoContext();
            if (s_dic.ContainsKey(key.Id) && s_dic[key.Id].ContainsKey(key.Key) && s_dic[key.Id][key.Key].ContainsKey(key.ValKey))
            {
                s_dic[key.Id][key.Key][key.ValKey] = value;
                return true;
            }
            return false;
        }

        public IAsyncEnumerable<Entity<ClassAnimal, ClassAnimalKey>> QueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
