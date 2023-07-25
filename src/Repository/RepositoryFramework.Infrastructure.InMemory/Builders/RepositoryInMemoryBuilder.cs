using System.Linq.Expressions;
using System.Population.Random;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.InMemory
{
    internal sealed class RepositoryInMemoryBuilder<T, TKey> : IRepositoryInMemoryBuilder<T, TKey>
        where TKey : notnull
    {
        public IServiceCollection Services { get; }
        private readonly ICommandPattern<T, TKey>? _commandPattern;
        public RepositoryInMemoryBuilder(IServiceCollection services, string factoryName)
        {
            Services = services;
            var serviceProvider = Services.BuildServiceProvider().CreateScope().ServiceProvider;
            var factory = serviceProvider.GetService<IFactory<IRepositoryPattern<T, TKey>>>();
            if (factory != null && factory.Exists(factoryName))
                _commandPattern = factory.Create(factoryName);
            else
            {
                var commandFactory = serviceProvider.GetService<IFactory<ICommandPattern<T, TKey>>>();
                if (commandFactory != null && commandFactory.Exists(factoryName))
                    _commandPattern = commandFactory.Create(factoryName);
                else
                {
                    var queryFactory = serviceProvider.GetService<IFactory<IQueryPattern<T, TKey>>>();
                    if (queryFactory != null && queryFactory.Exists(factoryName))
                        _commandPattern = queryFactory.Create(factoryName) as InMemoryStorage<T, TKey>;
                }
            }
        }
        private void AddElementBasedOnGenericElements(TKey key, T value)
        {
            if (_commandPattern != null)
                _commandPattern.InsertAsync(key, value).ToResult();
        }
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
