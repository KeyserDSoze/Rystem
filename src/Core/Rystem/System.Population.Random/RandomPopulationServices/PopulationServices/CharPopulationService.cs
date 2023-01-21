using System.Security.Cryptography;

namespace System.Population.Random
{
    internal class CharPopulationService : IRandomPopulationService
    {
        public int Priority => 1;

        public dynamic GetValue(PopulationSettings settings, RandomPopulationOptions options)
            => (char)RandomNumberGenerator.GetInt32(256);

        public bool IsValid(Type type)
            => type == typeof(char) || type == typeof(char?);
    }
}
