using System.Security.Cryptography;

namespace System.Population.Random
{
    internal class TimePopulationService : IRandomPopulationService
    {
        public int Priority => 1;

        public dynamic GetValue(PopulationSettings settings, RandomPopulationOptions options)
        {
            if (options.Type == typeof(DateTime) || options.Type == typeof(DateTime?))
                return DateTime.UtcNow;
            else if (options.Type == typeof(TimeSpan) || options.Type == typeof(TimeSpan?))
                return TimeSpan.FromTicks(RandomNumberGenerator.GetInt32(200_000));
            else
                return DateTimeOffset.UtcNow;
        }

        public bool IsValid(Type type)
            => type == typeof(DateTime) || type == typeof(DateTime?) || type == typeof(TimeSpan)
            || type == typeof(TimeSpan?) || type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?);
    }
}
