using System.Linq.Expressions;

namespace System.Population.Random
{
    internal sealed class PopulationBuilder<T> : IPopulationBuilder<T>
    {
        private readonly IPopulationStrategy<T> _populationStrategy;
        private readonly PopulationSettings<T> _settings;
        public PopulationBuilder(IPopulationStrategy<T> populationStrategy, PopulationSettings<T>? settings)
        {
            _populationStrategy = populationStrategy;
            _settings = settings ?? new();
        }
        private const string LinqFirst = "First().";

        private static string GetNameOfProperty<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
            => string.Join(".", navigationPropertyPath.ToString().Split('.').Skip(1)).Replace(LinqFirst, string.Empty);
        public IPopulationBuilder<T> WithPattern<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath, params string[] regex)
        {
            var nameOfProperty = GetNameOfProperty(navigationPropertyPath);
            var dictionary = _settings.RegexForValueCreation;
            if (dictionary.ContainsKey(nameOfProperty))
                dictionary[nameOfProperty] = regex;
            else
                dictionary.Add(nameOfProperty, regex);
            return this;
        }
        public IPopulationBuilder<T> WithSpecificNumberOfElements<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath, int numberOfElements)
        {
            var nameOfProperty = GetNameOfProperty(navigationPropertyPath);
            var dictionary = _settings.NumberOfElements;
            if (dictionary.ContainsKey(nameOfProperty))
                dictionary[nameOfProperty] = numberOfElements;
            else
                dictionary.Add(nameOfProperty, numberOfElements);
            return this;
        }
        public IPopulationBuilder<T> WithValue<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath, Func<TProperty> creator)
        {
            var nameOfProperty = GetNameOfProperty(navigationPropertyPath);
            var dictionary = _settings.DelegatedMethodForValueCreation;
            if (dictionary.ContainsKey(nameOfProperty))
                dictionary[nameOfProperty] = () => creator.Invoke()!;
            else
                dictionary.Add(nameOfProperty, () => creator.Invoke()!);
            return this;
        }
        public IPopulationBuilder<T> WithValue<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath, Func<IServiceProvider, Task<TProperty>> valueRetriever)
        {
            var nameOfProperty = GetNameOfProperty(navigationPropertyPath);
            var dictionary = _settings.DelegatedMethodForValueRetrieving;
            if (dictionary.ContainsKey(nameOfProperty))
                dictionary[nameOfProperty] = async (x) => (await valueRetriever.Invoke(x).NoContext())!;
            else
                dictionary.Add(nameOfProperty, async (x) => (await valueRetriever.Invoke(x).NoContext())!);
            return this;
        }
        public IPopulationBuilder<T> WithRandomValue<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath,
            Func<IServiceProvider, Task<IEnumerable<TProperty>>> valuesRetriever)
        {
            var nameOfProperty = GetNameOfProperty(navigationPropertyPath);
            var dictionary = _settings.DelegatedMethodWithRandomForValueRetrieving;
            if (dictionary.ContainsKey(nameOfProperty))
                dictionary[nameOfProperty] = async (x) => (await valuesRetriever.Invoke(x).NoContext()!).Select(x => (object)x!)!;
            else
                dictionary.Add(nameOfProperty, async (x) => (await valuesRetriever.Invoke(x).NoContext()!).Select(x => (object)x!)!);
            return this;
        }
        public IPopulationBuilder<T> WithRandomValue<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> navigationPropertyPath,
           Func<IServiceProvider, Task<IEnumerable<TProperty>>> valuesRetriever)
        {
            var nameOfProperty = GetNameOfProperty(navigationPropertyPath);
            var dictionary = _settings.DelegatedMethodWithRandomForValueRetrieving;
            if (dictionary.ContainsKey(nameOfProperty))
                dictionary[nameOfProperty] = async (x) => (await valuesRetriever.Invoke(x).NoContext()!).Select(x => (object)x!)!;
            else
                dictionary.Add(nameOfProperty, async (x) => (await valuesRetriever.Invoke(x).NoContext()!).Select(x => (object)x!)!);
            return this;
        }
        public IPopulationBuilder<T> WithAutoIncrement<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath, TProperty start)
        {
            var nameOfProperty = GetNameOfProperty(navigationPropertyPath);
            var dictionary = _settings.AutoIncrementations;
            if (dictionary.ContainsKey(nameOfProperty))
                dictionary[nameOfProperty] = start!;
            else
                dictionary.Add(nameOfProperty, start!);
            return this;
        }
        public IPopulationBuilder<T> WithImplementation<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath, Type implementationType)
        {
            var nameOfProperty = GetNameOfProperty(navigationPropertyPath);
            var dictionary = _settings.ImplementationForValueCreation;
            if (dictionary.ContainsKey(nameOfProperty))
                dictionary[nameOfProperty] = implementationType;
            else
                dictionary.Add(nameOfProperty, implementationType);
            return this;
        }
        public IPopulationBuilder<T> WithImplementation<TProperty, TEntity>(Expression<Func<T, TProperty>> navigationPropertyPath)
            => WithImplementation(navigationPropertyPath, typeof(TEntity));
        public List<T> Populate(int numberOfElements = 100, int numberOfElementsWhenEnumerableIsFound = 10) 
            => _populationStrategy.Populate(_settings, numberOfElements, numberOfElementsWhenEnumerableIsFound);
    }
}
