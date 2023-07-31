using System.Reflection;

namespace System.Text.Minimization
{
    internal class ObjectSerializer : IMinimizationInterpreter
    {
        public int Priority => 0;
        private static readonly Type s_ignore = typeof(MinimizationIgnore);
        public bool IsValid(Type type) => !type.IsInterface && !type.IsAbstract;
        private static readonly Dictionary<string, PropertyInfo[]> s_properties = new();
        private static readonly object s_semaphore = new();
        private static readonly Type s_csvProperty = typeof(MinimizationPropertyAttribute);
        private static PropertyInfo[] GetOrderedProperties(Type type)
        {
            if (!s_properties.ContainsKey(type.FullName!))
                lock (s_semaphore)
                    if (!s_properties.ContainsKey(type.FullName!))
                        s_properties.Add(type.FullName!, type.FetchProperties(s_ignore)
                            .OrderBy(x => (x.GetCustomAttribute(s_csvProperty) as MinimizationPropertyAttribute)?.Column ?? int.MaxValue)
                            .ToArray());
            return s_properties[type.FullName!];
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
