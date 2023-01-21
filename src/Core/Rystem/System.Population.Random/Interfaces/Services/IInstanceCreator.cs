namespace System.Population.Random
{
    /// <summary>
    /// Interface that helps the creation of a new instance of object during random population.
    /// </summary>
    public interface IInstanceCreator
    {
        object? CreateInstance(PopulationSettings settings, RandomPopulationOptions options, object?[]? args = null);
    }
}