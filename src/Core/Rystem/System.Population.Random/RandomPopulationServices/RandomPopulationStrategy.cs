namespace System.Population.Random
{
    internal sealed class RandomPopulationStrategy<T> : IPopulationStrategy<T>
    {
        private readonly IPopulationService _populationService;

        public RandomPopulationStrategy(IPopulationService populationService)
        {
            _populationService = populationService;
        }
        public List<T> Populate(PopulationSettings<T>? settings = null, int numberOfElements = 100, int numberOfElementsWhenEnumerableIsFound = 10)
        {
            List<T> items = new();
            settings ??= new();
            for (var i = 0; i < numberOfElements; i++)
            {
                var entity = _populationService.Construct(settings, typeof(T),
                        numberOfElementsWhenEnumerableIsFound, string.Empty, string.Empty);
                var item = (T)entity!;
                items.Add(item);
            }
            return items;
        }
    }
}
