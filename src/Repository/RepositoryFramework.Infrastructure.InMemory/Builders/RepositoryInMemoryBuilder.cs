using System.Linq.Expressions;
using System.Population.Random;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.InMemory
{
    internal sealed class RepositoryInMemoryBuilder<T, TKey> : IRepositoryInMemoryBuilder<T, TKey>
        where TKey : notnull
    {
        public IServiceCollection Services => Builder.Services;
        public IRepositoryBuilder<T, TKey, IRepository<T, TKey>> Builder { get; }
        public RepositoryInMemoryBuilder(IRepositoryBuilder<T, TKey, IRepository<T, TKey>> builder)
        {
            Builder = builder;
        }
        private void AddElementBasedOnGenericElements(TKey key, T value)
            => InMemoryStorage<T, TKey>.AddValue(key, value);
        public IRepositoryInMemoryBuilder<T, TKey> PopulateWithJsonData(
            Expression<Func<T, TKey>> navigationKey,
            string json)
        {
            var elements = json.FromJson<IEnumerable<T>>();
            if (elements != null)
                return PopulateWithDataInjection(navigationKey, elements);
            return this;
        }
        public IRepositoryInMemoryBuilder<T, TKey> PopulateWithDataInjection(
            Expression<Func<T, TKey>> navigationKey,
            IEnumerable<T> elements)
        {
            var keyProperty = navigationKey.GetPropertyBasedOnKey();
            foreach (var element in elements)
                AddElementBasedOnGenericElements((TKey)keyProperty.GetValue(element)!, element);
            return this;
        }
        public IPopulationBuilder<Entity<T, TKey>> PopulateWithRandomData(
            int numberOfElements = 100,
            int numberOfElementsWhenEnumerableIsFound = 10)
        {
            Services.AddPopulationService();
            Services.AddWarmUp(serviceProvider =>
            {
                var populationStrategy = serviceProvider.GetService<IPopulation<Entity<T, TKey>>>();
                if (populationStrategy != null)
                {
                    var elements = populationStrategy
                        .Populate(numberOfElements, numberOfElementsWhenEnumerableIsFound);
                    foreach (var element in elements)
                        AddElementBasedOnGenericElements(element.Key!, element.Value!);
                }
                return Task.CompletedTask;
            });
            return Services.AddPopulationSettings<Entity<T, TKey>>();
        }
    }
}
