using System.Linq.Expressions;
using System.Population.Random;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.InMemory
{
    internal sealed class RepositoryInMemoryBuilder<T, TKey> : IRepositoryInMemoryBuilder<T, TKey>, IOptionsBuilder<RepositoryBehaviorSettings<T, TKey>>
        where TKey : notnull
    {
        internal string FactoryName { get; set; }
        internal IServiceCollection Services { get; set; }
        public RepositoryBehaviorSettings<T, TKey> Settings { get; } = new();
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
            if (factory != null && factory.Exists(FactoryName))
                commandPattern = factory.Create(FactoryName);
            else
            {
                var commandFactory = serviceProvider.GetService<IFactory<ICommandPattern<T, TKey>>>();
                if (commandFactory != null && commandFactory.Exists(FactoryName))
                    commandPattern = commandFactory.Create(FactoryName);
                else
                {
                    var queryFactory = serviceProvider.GetService<IFactory<IQueryPattern<T, TKey>>>();
                    if (queryFactory != null && queryFactory.Exists(FactoryName))
                        commandPattern = queryFactory.Create(FactoryName) as InMemoryStorage<T, TKey>;
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
        private void CheckSettings()
        {
            Check(Settings.Get(RepositoryMethods.Insert).ExceptionOdds);
            Check(Settings.Get(RepositoryMethods.Update).ExceptionOdds);
            Check(Settings.Get(RepositoryMethods.Delete).ExceptionOdds);
            Check(Settings.Get(RepositoryMethods.Batch).ExceptionOdds);
            Check(Settings.Get(RepositoryMethods.Get).ExceptionOdds);
            Check(Settings.Get(RepositoryMethods.Query).ExceptionOdds);
            Check(Settings.Get(RepositoryMethods.Exist).ExceptionOdds);
            Check(Settings.Get(RepositoryMethods.Operation).ExceptionOdds);
            Check(Settings.Get(RepositoryMethods.All).ExceptionOdds);

            static void Check(List<ExceptionOdds> odds)
            {
                var total = odds.Sum(x => x.Percentage);
                if (odds.Any(x => x.Percentage <= 0 || x.Percentage > 100))
                {
                    throw new ArgumentException("Some percentages are wrong, greater than 100% or lesser than 0.");
                }
                if (total > 100)
                    throw new ArgumentException("Your total percentage is greater than 100.");
            }
        }
        public Func<IServiceProvider, RepositoryBehaviorSettings<T, TKey>> Build()
        {
            CheckSettings();
            return serviceProvider => Settings;
        }
    }
}
