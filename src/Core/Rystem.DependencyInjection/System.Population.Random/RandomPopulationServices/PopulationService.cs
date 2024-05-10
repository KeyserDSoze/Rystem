using System.Collections;
using System.Reflection;

namespace System.Population.Random
{
    internal class PopulationService : IPopulationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<IRandomPopulationService> _randomPopulationServices;
        private readonly IRegexService _regexService;
        public PopulationService(
            IServiceProvider serviceProvider,
            IEnumerable<IRandomPopulationService> randomPopulationServices,
            IRegexService regexService)
        {
            _serviceProvider = serviceProvider;
            _randomPopulationServices = randomPopulationServices;
            _regexService = regexService;
        }
        private static readonly Dictionary<string, int> s_indexes = new();
        private static readonly Dictionary<string, List<dynamic>> s_randomValuesFromRystem = new();
        private string GetRandomValueFromRystem(RandomPopulationFromRystemSettings randomSettings, PopulationSettings settings, Type type, string treeName)
        {
            var numberOfElements = settings.NumberOfElements;
            var treeNameForType = $"{randomSettings.StartingType.FullName}_{treeName}";
            var randomValueKey = randomSettings.ForcedKey;
            if (randomValueKey == null)
                randomValueKey = randomSettings.UseTheSameRandomValuesForTheSameType ? type.FullName! : treeNameForType;
            if (!s_randomValuesFromRystem.ContainsKey(randomValueKey) || s_randomValuesFromRystem[randomValueKey].Count < numberOfElements)
            {
                var startingFrom = 0;
                if (s_randomValuesFromRystem.ContainsKey(randomValueKey) && s_randomValuesFromRystem[randomValueKey].Count < numberOfElements)
                {
                    startingFrom = s_randomValuesFromRystem[randomValueKey].Count;
                }
                else
                {
                    s_randomValuesFromRystem.Add(randomValueKey, new());
                }
                for (var i = startingFrom; i < numberOfElements; i++)
                {
                    if (randomSettings.Creator == null)
                    {
                        var service = _randomPopulationServices.OrderByDescending(x => x.Priority).FirstOrDefault(x => x.IsValid(type));
                        if (service != default)
                            s_randomValuesFromRystem[randomValueKey].Add(service.GetValue(settings, new RandomPopulationOptions(type, this, numberOfElements, treeName, null!)));
                    }
                    else
                    {
                        s_randomValuesFromRystem[randomValueKey].Add(randomSettings.Creator.Invoke());
                    }
                }
            }
            if (!s_indexes.ContainsKey(treeNameForType))
            {
                s_indexes.Add(treeNameForType, 0);
            }
            var s_ids = s_randomValuesFromRystem[randomValueKey];
            var id = s_ids[s_indexes[treeNameForType]];
            s_indexes[treeNameForType]++;
            if (s_indexes[treeNameForType] >= numberOfElements)
                s_indexes[treeNameForType] = 0;
            return id;
        }
        public dynamic? Construct(PopulationSettings settings, Type type, int numberOfEntities, string treeName, string name, List<Type>? notCompletelyConstructedNonPrimitiveTypes)
        {
            try
            {
                notCompletelyConstructedNonPrimitiveTypes ??= new();
                if (!type.IsPrimitive() && notCompletelyConstructedNonPrimitiveTypes.Any(x => x == type))
                    return default;
                if (!type.IsPrimitive())
                    notCompletelyConstructedNonPrimitiveTypes.Add(type);

                if (!string.IsNullOrWhiteSpace(treeName) && !string.IsNullOrWhiteSpace(name))
                    treeName = $"{treeName}.{name}";
                else if (!string.IsNullOrWhiteSpace(name))
                    treeName = name;

                int? overridedNumberOfEntities = null;
                var numberOfEntitiesDictionary = settings.ForcedNumberOfElementsForEnumerable;
                if (numberOfEntitiesDictionary.ContainsKey(treeName))
                    overridedNumberOfEntities = numberOfEntitiesDictionary[treeName];
                numberOfEntities = overridedNumberOfEntities ?? numberOfEntities;

                if (settings.DelegatedMethodForValueCreation.ContainsKey(treeName))
                    return settings.DelegatedMethodForValueCreation[treeName].Invoke();

                if (settings.DelegatedMethodForValueRetrieving.ContainsKey(treeName))
                    return settings.DelegatedMethodForValueRetrieving[treeName].Invoke(_serviceProvider).ToResult();

                if (settings.DelegatedMethodWithRandomForValueRetrieving.ContainsKey(treeName))
                {
                    var entities = settings.DelegatedMethodWithRandomForValueRetrieving[treeName].Invoke(_serviceProvider).ToResult();
                    var value = (Activator.CreateInstance(type) as IList)!;
                    var count = entities.Count() - numberOfEntities;
                    var index = System.Random.Shared.Next(0, count);
                    foreach (var entity in entities.Skip(index).Take(numberOfEntities))
                        value.Add(entity);
                    return value;
                }

                if (settings.RegexForValueCreation.ContainsKey(treeName))
                    return _regexService.GetRandomValue(type,
                        settings.RegexForValueCreation[treeName]);

                if (settings.AutoIncrementations.ContainsKey(treeName))
                    return settings.AutoIncrementations[treeName]++;

                if (settings.RandomValueFromRystem.ContainsKey(treeName))
                    return GetRandomValueFromRystem(settings.RandomValueFromRystem[treeName], settings, type, treeName);

                if (settings.ImplementationForValueCreation.ContainsKey(treeName) && !string.IsNullOrWhiteSpace(name))
                    return Construct(settings, settings.ImplementationForValueCreation[treeName], numberOfEntities,
                        treeName, string.Empty, notCompletelyConstructedNonPrimitiveTypes);

                var service = _randomPopulationServices.OrderByDescending(x => x.Priority).FirstOrDefault(x => x.IsValid(type));
                if (service != default)
                    return service.GetValue(settings, new RandomPopulationOptions(type, this, numberOfEntities, treeName, notCompletelyConstructedNonPrimitiveTypes));
                return default;
            }
            catch
            {
                return default;
            }
            finally
            {
                ReleaseNonPrimitiveType();
            }
            void ReleaseNonPrimitiveType()
            {
                if (!type.IsPrimitive())
                    notCompletelyConstructedNonPrimitiveTypes!.RemoveAll(x => x == type);
            }
        }
    }
}
