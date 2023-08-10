using System.Reflection;

namespace System.Population.Random
{
    public class InstanceCreator : IInstanceCreator
    {
        private static readonly List<Type> s_defaultPrimitive = new()
        {
            typeof(Range)
        };
        public object? CreateInstance(PopulationSettings settings, RandomPopulationOptions options, object?[]? args = null)
        {
            if (options.Type.IsPrimitive() || s_defaultPrimitive.Contains(options.Type))
                return options.PopulationService.Construct(settings, options.Type,
                        options.NumberOfEntities, options.TreeName, string.Empty);

            var constructor = options.Type.GetConstructors()
                .OrderBy(x => x.GetParameters().Length)
                .FirstOrDefault();
            if (constructor == null)
            {
                try
                {
                    return options.PopulationService.Construct(settings, options.Type,
                     options.NumberOfEntities, options.TreeName, string.Empty);
                }
                catch
                {
                    return null;
                }
            }
            else if (constructor.GetParameters().Length == 0)
                return Activator.CreateInstance(options.Type, args);
            else
            {
                List<object> instances = new();
                foreach (var x in constructor.GetParameters())
                    instances.Add(options.PopulationService.Construct(settings, x.ParameterType,
                        options.NumberOfEntities, options.TreeName, $"{x.Name![0..1].ToUpper()}{x.Name[1..]}"));
                return Activator.CreateInstance(options.Type, instances.ToArray());
            }
        }
    }
}
