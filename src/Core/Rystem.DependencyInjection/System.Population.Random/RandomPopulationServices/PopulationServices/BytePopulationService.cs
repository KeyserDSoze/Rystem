using System.Security.Cryptography;

namespace System.Population.Random
{
    internal class BytePopulationService : IRandomPopulationService
    {
        public int Priority => 1;

        public dynamic GetValue(PopulationSettings settings, RandomPopulationOptions options)
        {
            if (options.Type == typeof(byte) || options.Type == typeof(byte?))
                return RandomNumberGenerator.GetBytes(1)[0];
            else
                return (sbyte)RandomNumberGenerator.GetBytes(1)[0];
        }

        public bool IsValid(Type type)
            => type == typeof(byte) || type == typeof(byte?) || type == typeof(sbyte) || type == typeof(sbyte?);
    }
}
