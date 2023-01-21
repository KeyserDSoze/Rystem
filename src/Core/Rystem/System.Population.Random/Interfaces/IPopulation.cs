namespace System.Population.Random
{
    public interface IPopulation<T>
    {
        IPopulationBuilder<T> Setup(PopulationSettings<T>? settings = null);
        List<T> Populate(int numberOfElements = 100, int numberOfElementsWhenEnumerableIsFound = 10);
    }
}
