using System;
using System.Linq.Expressions;
using System.Population.Random;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.InMemory
{
    internal sealed class RepositoryInMemoryBuilder<T, TKey> : IRepositoryInMemoryBuilder<T, TKey>
        where TKey : notnull
    {
        private readonly string _factoryName;

        public IServiceCollection Services { get; }
        public RepositoryInMemoryBuilder(IServiceCollection services, string factoryName)
        {
            Services = services;
            _factoryName = factoryName;
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
            Services.AddWarmUp(async serviceProvider =>
            {
                var commandPattern = GetCommandPattern(serviceProvider);
                foreach (var element in elements)
                    await commandPattern.InsertAsync((TKey)keyProperty.GetValue(element)!, element).NoContext();
            });
            return this;
        }
        private ICommandPattern<T, TKey> GetCommandPattern(IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetService<IFactory<IRepositoryPattern<T, TKey>>>();
            ICommandPattern<T, TKey>? commandPattern = null;
            if (factory != null && factory.Exists(_factoryName))
                commandPattern = factory.Create(_factoryName);
            else
            {
                var commandFactory = serviceProvider.GetService<IFactory<ICommandPattern<T, TKey>>>();
                if (commandFactory != null && commandFactory.Exists(_factoryName))
                    commandPattern = commandFactory.Create(_factoryName);
                else
                {
                    var queryFactory = serviceProvider.GetService<IFactory<IQueryPattern<T, TKey>>>();
                    if (queryFactory != null && queryFactory.Exists(_factoryName))
                        commandPattern = queryFactory.Create(_factoryName) as InMemoryStorage<T, TKey>;
                }
            }
            return commandPattern!;
        }
        public IPopulationBuilder<Entity<T, TKey>> PopulateWithRandomData(
            int numberOfElements = 100,
            int numberOfElementsWhenEnumerableIsFound = 10)
        {
            Services.AddPopulationService();
            Services.AddWarmUp(async serviceProvider =>
            {
                var populationStrategy = serviceProvider.GetService<IPopulation<Entity<T, TKey>>>();
                var commandPattern = GetCommandPattern(serviceProvider);
                if (populationStrategy != null)
                {
                    var elements = populationStrategy
                        .Populate(numberOfElements, numberOfElementsWhenEnumerableIsFound);
                    foreach (var element in elements)
                        await commandPattern.InsertAsync(element.Key!, element.Value!).NoContext();
                }
            });
            return Services.AddPopulationSettings<Entity<T, TKey>>();
        }
    }
}
