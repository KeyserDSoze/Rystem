namespace System.Population.Random
{
    internal class ObjectPopulationService : IRandomPopulationService
    {
        public int Priority => 0;

        public dynamic GetValue(PopulationSettings settings, RandomPopulationOptions options)
        {
            if (!options.Type.IsInterface && !options.Type.IsAbstract)
            {
                object? entity = null;
                var constructor = options.Type.GetConstructors()
                .OrderByDescending(x => x.GetParameters().Length)
                .FirstOrDefault();
                if (constructor == null)
                {
                    try
                    {
                        entity = options.PopulationService.Construct(settings, options.Type,
                         options.NumberOfEntities, options.TreeName, string.Empty);
                    }
                    catch
                    {
                        entity = null;
                    }
                }
                else if (constructor.GetParameters().Length == 0)
                    entity = constructor.Invoke(Array.Empty<object>());
                else
                {
                    List<object> instances = new();
                    foreach (var x in constructor.GetParameters())
                        instances.Add(options.PopulationService.Construct(settings, x.ParameterType,
                            options.NumberOfEntities, options.TreeName, $"{x.Name![0..1].ToUpper()}{x.Name[1..]}"));
                    entity = constructor.Invoke(instances.ToArray());
                }
                if (entity != null)
                {
                    var properties = options.Type.GetProperties();
                    foreach (var property in properties.Where(x => x.SetMethod != null))
                    {
                        _ = Try.WithDefaultOnCatch(() =>
                        {
                            var value = options.PopulationService
                                    .Construct(settings,
                                               property.PropertyType,
                                               options.NumberOfEntities,
                                               options.TreeName,
                                               property.Name);
                            property
                                .SetValue(entity, value);
                        });
                    }
                }
                return entity!;
            }
            return default!;
        }

        public bool IsValid(Type type)
            => !type.IsInterface && !type.IsAbstract;
    }
}
