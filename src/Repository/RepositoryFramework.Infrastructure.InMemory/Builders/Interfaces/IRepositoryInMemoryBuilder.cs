using System.Linq.Expressions;
using System.Population.Random;

namespace RepositoryFramework.InMemory
{
    public interface IRepositoryInMemoryBuilder<T, TKey>
        where TKey : notnull
    {
        RepositoryBehaviorSettings<T, TKey> Settings { get; }
        IRepositoryInMemoryBuilder<T, TKey> PopulateWithJsonData(
            Expression<Func<T, TKey>> navigationKey,
            string json);
        IRepositoryInMemoryBuilder<T, TKey> PopulateWithDataInjection(
            Expression<Func<T, TKey>> navigationKey,
            IEnumerable<T> elements);
        IPopulationBuilder<Entity<T, TKey>> PopulateWithRandomData(
            int numberOfElements = 100,
            int numberOfElementsWhenEnumerableIsFound = 10);
    }
}
