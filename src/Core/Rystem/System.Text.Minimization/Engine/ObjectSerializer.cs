using System.Reflection;

namespace System.Text.Minimization
{
    internal class ObjectSerializer : IMinimizationInterpreter
    {
        public int Priority => 0;
        private static readonly Type Ignore = typeof(MinimizationIgnore);
        public bool IsValid(Type type) => !type.IsInterface && !type.IsAbstract;
        private static readonly Dictionary<string, PropertyInfo[]> Properties = new();
        private static readonly object Semaphore = new();
        private static readonly Type CsvProperty = typeof(MinimizationPropertyAttribute);
        private static PropertyInfo[] GetOrderedProperties(Type type)
        {
            if (!Properties.ContainsKey(type.FullName!))
                lock (Semaphore)
                    if (!Properties.ContainsKey(type.FullName!))
                        Properties.Add(type.FullName!, type.FetchProperties(Ignore)
                            .OrderBy(x => (x.GetCustomAttribute(CsvProperty) as MinimizationPropertyAttribute)?.Column ?? int.MaxValue)
                            .ToArray());
            return Properties[type.FullName!];
        }
        public dynamic Deserialize(Type type, string value, int deep = int.MaxValue)
        {
            var constructor = type.FecthConstructors()
               .OrderBy(x => x.GetParameters().Length)
               .FirstOrDefault();
            if (constructor == null)
                return null!;
            else
            {
                var instance = Activator.CreateInstance(type, constructor.GetParameters().Select(x => x.DefaultValue!).ToArray())!;
                var enumerator = value.Split((char)deep).GetEnumerator();
                foreach (var property in GetOrderedProperties(type))
                {
                    enumerator.MoveNext();
                    if (property.SetMethod != null)
                        property.SetValue(instance, Serializer.Instance.Deserialize(property.PropertyType, enumerator.Current.ToString()!, deep - 1));
                }
                return instance;
            }
        }
        public string Serialize(Type type, object value, int deep)
            => string.Join((char)deep,
                GetOrderedProperties(type)
                    .Select(x => Serializer.Instance.Serialize(x.PropertyType, x.GetValue(value)!, deep - 1)));
    }
}
