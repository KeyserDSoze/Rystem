namespace System.Population.Random
{
    internal sealed class RandomPopulation<T> : IPopulation<T>
    {
        private readonly IPopulationStrategy<T> _strategy;
        private readonly PopulationSettings<T> _settings;
        public RandomPopulation(IPopulationStrategy<T> strategy, PopulationSettings<T>? settings = null)
        {
            _strategy = strategy;
            _settings = settings ?? new();
        }
        public IPopulationBuilder<T> Setup(PopulationSettings<T>? settings = null)
            => new PopulationBuilder<T>(_strategy, settings ?? _settings);
        public List<T> Populate(int numberOfElements = 100, int numberOfElementsWhenEnumerableIsFound = 10)
            => _strategy.Populate(_settings, numberOfElements, numberOfElementsWhenEnumerableIsFound);
    }
}
