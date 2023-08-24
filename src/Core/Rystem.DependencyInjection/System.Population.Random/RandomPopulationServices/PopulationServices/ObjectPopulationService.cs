using System.Reflection;

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
                         options.NumberOfEntities, options.TreeName, string.Empty, options.NotAlreadyConstructedNonPrimitiveTypes);
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
                    foreach (var parameter in constructor.GetParameters())
                    {
                        instances.Add(options.PopulationService.Construct(settings, parameter.ParameterType,
                            options.NumberOfEntities, options.TreeName, $"{parameter.Name![0..1].ToUpper()}{parameter.Name[1..]}",
                            options.NotAlreadyConstructedNonPrimitiveTypes));
                    }
                    entity = constructor.Invoke(instances.ToArray());
                }
                if (entity != null)
                {
                    var properties = options.Type.GetProperties();
                    foreach (var property in properties.Where(x => x.SetMethod != null))
                    {
                        _ = Try.WithDefaultOnCatch(() =>
                        {
                            var value = property.GetValue(entity, null);
                            var defaultValue = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
                            if (value == null || value.Cast(property.PropertyType).Equals(defaultValue.Cast(property.PropertyType)))
                            {
                                value = options.PopulationService
                                        .Construct(settings,
                                                   property.PropertyType,
                                                   options.NumberOfEntities,
                                                   options.TreeName,
                                                   property.Name,
                                                   options.NotAlreadyConstructedNonPrimitiveTypes);
                                property
                                    .SetValue(entity, value);
                            }
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
