namespace System.Population.Random
{
    /// <summary>
    /// Model for random population service.
    /// </summary>
    /// <param name="Type">Entity type of model to populate</param>
    /// <param name="PopulationService">IPopulationService</param>
    /// <param name="NumberOfEntities">In case of Enumerable or Array, number of elements to create</param>
    /// <param name="TreeName">The name that represents the history of creation of this object.
    /// For example, if you are creating an object in a property B of another object A, you may find it valued
    /// as "A.B"</param>
    public record RandomPopulationOptions(Type Type,
        IPopulationService PopulationService, int NumberOfEntities,
        string TreeName);
}
