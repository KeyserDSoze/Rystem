using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace System.Population.Random
{
    internal sealed class RandomPopulationStrategy<T> : IPopulationStrategy<T>
    {
        private readonly IPopulationService _populationService;
        private readonly IInstanceCreator _instanceCreator;

        public RandomPopulationStrategy(IPopulationService populationService,
            IInstanceCreator instanceCreator)
        {
            _populationService = populationService;
            _instanceCreator = instanceCreator;
        }
        public List<T> Populate(PopulationSettings<T>? settings = null, int numberOfElements = 100, int numberOfElementsWhenEnumerableIsFound = 10)
        {
            List<T> items = new();
            settings ??= new();
            var properties = typeof(T).GetProperties();
            for (var i = 0; i < numberOfElements; i++)
            {
                var entity = _instanceCreator!.CreateInstance(settings, new RandomPopulationOptions(typeof(T),
                    _populationService!, numberOfElementsWhenEnumerableIsFound, string.Empty));
                foreach (var property in properties.Where(x => x.CanWrite))
                {
                    if (property.PropertyType == typeof(Range) ||
                            GetDefault(property.PropertyType) == (property.GetValue(entity) as dynamic))
                    {
                        var value = _populationService!.Construct(settings, property.PropertyType,
                            numberOfElementsWhenEnumerableIsFound, string.Empty,
                            property.Name);
                        property.SetValue(entity, value);
                    }
                }
                var item = (T)entity!;
                items.Add(item);
            }
            return items;
        }
        private static dynamic GetDefault(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type)!;
            return null!;
        }
    }
}
