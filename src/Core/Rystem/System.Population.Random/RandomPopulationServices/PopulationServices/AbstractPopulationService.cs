using System.Reflection;

namespace System.Population.Random
{
    internal class AbstractPopulationService : IRandomPopulationService
    {
        public int Priority => 0;

        public dynamic GetValue(PopulationSettings settings, RandomPopulationOptions options)
        {
            try
            {
                return options.PopulationService.Construct(settings, options.Type.Mock()!,
                    options.NumberOfEntities, options.TreeName, string.Empty)!;
            }
            catch
            {
                return null!;
            }
        }

        public bool IsValid(Type type)
            => type.IsAbstract;

    }
}