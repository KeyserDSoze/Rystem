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
                var numberOfEntitiesDictionary = settings.NumberOfElements;
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
