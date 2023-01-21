namespace System.Population.Random
{
    internal class GuidPopulationService : IRandomPopulationService
    {
        public int Priority => 1;

        public dynamic GetValue(PopulationSettings settings, RandomPopulationOptions options)
            => Guid.NewGuid();

        public bool IsValid(Type type)
            => type == typeof(Guid) || type == typeof(Guid?);
    }
}
