using System.Security.Cryptography;

namespace System.Population.Random
{
    internal class BoolPopulationService : IRandomPopulationService
    {
        public int Priority => 1;

        public dynamic GetValue(PopulationSettings settings, RandomPopulationOptions options)
            => RandomNumberGenerator.GetInt32(4) > 1;

        public bool IsValid(Type type)
            => type == typeof(bool) || type == typeof(bool?);
    }
}
