using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace System.Population.Random
{
    internal class DictionaryPopulationService : IRandomPopulationService
    {
        public int Priority => 3;

        public dynamic GetValue(PopulationSettings settings, RandomPopulationOptions options)
        {
            var keyType = options.Type.GetGenericArguments().First();
            var valueType = options.Type.GetGenericArguments().Last();
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            var entity = Activator.CreateInstance(dictionaryType)! as IDictionary;
            for (var i = 0; i < options.NumberOfEntities; i++)
            {
                var newKey = options.PopulationService.Construct(settings, options.Type.GetGenericArguments().First(),
                    options.NumberOfEntities, options.TreeName, "Key");
                var newValue = options.PopulationService.Construct(settings, options.Type.GetGenericArguments().Last(),
                    options.NumberOfEntities, options.TreeName, "Value");
                entity!.Add(newKey, newValue);
            }
            return entity!;
        }

        public bool IsValid(Type type)
        {
            if (!type.IsArray)
            {
                var interfaces = type.GetInterfaces();
                if (type.Name.Contains("IDictionary`2") || interfaces.Any(x => x.Name.Contains("IDictionary`2")))
                    return true;
            }
            return false;
        }
    }
}
