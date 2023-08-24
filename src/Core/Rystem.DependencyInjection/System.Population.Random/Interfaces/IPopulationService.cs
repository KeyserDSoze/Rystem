using System;

namespace System.Population.Random
{
    /// <summary>
    /// Population service.
    /// </summary>
    public interface IPopulationService
    {
        dynamic? Construct(PopulationSettings settings, Type type, int numberOfEntities, string treeName, string name, List<Type>? alreadyConstructedNonPrimitiveTypes);
    }
}
