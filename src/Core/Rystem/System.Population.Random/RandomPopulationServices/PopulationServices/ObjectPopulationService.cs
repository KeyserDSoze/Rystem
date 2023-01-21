namespace System.Population.Random
{
    internal class ObjectPopulationService : IRandomPopulationService
    {
        public int Priority => 0;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S108:Nested blocks of code should not be left empty", Justification = "I need this behavior because I don't want to stop the creation of a random object for one not allowed strange type.")]
        public dynamic GetValue(PopulationSettings settings, RandomPopulationOptions options)
        {
            if (!options.Type.IsInterface && !options.Type.IsAbstract)
            {
                var entity = options.PopulationService.InstanceCreator
                    .CreateInstance(settings, options);
                var properties = options.Type.GetProperties();
                foreach (var property in properties.Where(x => x.SetMethod != null))
                    try
                    {
                        var value = options.PopulationService
                                .Construct(settings,
                                           property.PropertyType,
                                           options.NumberOfEntities,
                                           options.TreeName,
                                           property.Name);
                        property
                            .SetValue(entity, value);
                    }
                    catch
                    {
                    }
                return entity!;
            }
            return default!;
        }

        public bool IsValid(Type type)
            => !type.IsInterface && !type.IsAbstract;
    }
}