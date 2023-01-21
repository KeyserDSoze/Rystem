using System.Collections.Generic;

namespace System.Population.Random
{
    public interface IPopulationStrategy<T>
    {
        List<T> Populate(PopulationSettings<T>? _settings = null, int numberOfElements = 100, int numberOfElementsWhenEnumerableIsFound = 10);
    }
}
