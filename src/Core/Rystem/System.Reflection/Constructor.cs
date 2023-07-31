using System.Collections.Concurrent;

namespace System.Reflection
{
    public static class Constructor
    {
        public static T? InvokeWithBestDynamicFit<T>(params object[] args)
        {
            var entity = typeof(T).ConstructWithBestDynamicFit(args);
            return entity != null ? (T)entity : default;
        }
        private sealed class Constructable
        {
            public List<int> ConstructorIndex { get; } = new();
            public ConstructorInfo? Constructor { get; set; }
            public List<int> PropertyIndex { get; } = new();
            public List<PropertyInfo> Properties { get; } = new();
            public object? Construct(params object[] args)
            {
                if (Constructor == null)
                    return null;
                var valuesForConstructor = new object[ConstructorIndex.Count];
                for (var i = 0; i < ConstructorIndex.Count; i++)
                {
                    valuesForConstructor[i] = args[ConstructorIndex[i]];
                }
                var entity = Constructor.Invoke(valuesForConstructor);
                if (Properties != null && Properties.Count > 0)
                {
                    var propertyIndex = PropertyIndex.GetEnumerator();
                    foreach (var property in Properties)
                    {
                        propertyIndex.MoveNext();
                        property.SetValue(entity, args[propertyIndex.Current]);
                    }
                }
                return entity;
            }
        }
        private static readonly ConcurrentDictionary<string, Constructable> s_constructors = new();
        public static object? ConstructWithBestDynamicFit(this Type type, params object[] args)
        {
            var argsKey = $"{type.FullName}_{string.Join("_", args.Select(x => x.GetType().FullName))}";
            if (!s_constructors.ContainsKey(argsKey))
            {
                var constructors = type.FecthConstructors();
                if (constructors.Length == 0)
                {
                    type = type.Mock()!;
                }
                constructors = type.FecthConstructors();
                foreach (var constructor in constructors.OrderByDescending(x => x.GetParameters().Length))
                {
                    var constructable = new Constructable();
                    var parameters = constructor.GetParameters();
                    List<int> constructorIndex = new();
                    foreach (var property in parameters)
                    {
                        for (var i = 0; i < args.Length; i++)
                            if (!constructorIndex.Contains(i) && args[i].GetType() == property.ParameterType)
                            {
                                constructorIndex.Add(i);
                                break;
                            }
                    }
                    constructable.Constructor = constructor;
                    constructable.ConstructorIndex.AddRange(constructorIndex);
                    if (constructorIndex.Count != parameters.Length)
                        continue;
                    if (constructorIndex.Count != args.Length)
                    {
                        var settableProperties = type.FetchProperties().Where(x => x.SetMethod != null).ToList();
                        List<int> propertyIndex = new();
                        for (var i = 0; i < args.Length; i++)
                        {
                            if (!constructorIndex.Contains(i) && !constructable.PropertyIndex.Contains(i))
                            {
                                for (var j = 0; j < settableProperties.Count; j++)
                                {
                                    if (!propertyIndex.Contains(j) && settableProperties[j].PropertyType == args[i].GetType())
                                    {
                                        propertyIndex.Add(j);
                                        constructable.PropertyIndex.Add(i);
                                        constructable.Properties.Add(settableProperties[j]);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    s_constructors.TryAdd(argsKey, constructable);
                    break;
                }
            }
            return s_constructors[argsKey].Construct(args);
        }
    }
}
