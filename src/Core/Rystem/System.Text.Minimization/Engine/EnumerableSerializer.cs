using System.Collections;

namespace System.Text.Minimization
{
    internal class EnumerableSerializer : IMinimizationInterpreter
    {
        public int Priority => 4;
        public bool IsValid(Type type)
        {
            if (!type.IsArray)
            {
                var interfaces = type.GetInterfaces();
                if (type.Name.Contains("IEnumerable`1") || interfaces.Any(x => x.Name.Contains("IEnumerable`1")))
                    return true;
            }
            return false;
        }
        public dynamic Deserialize(Type type, string value, int deep = int.MaxValue)
        {
            var currentType = type.GetGenericArguments().First();
            if (type.IsInterface)
                type = typeof(List<>).MakeGenericType(currentType);
            IList items = (Activator.CreateInstance(type) as IList)!;
            var list = value.Split((char)(deep));
            for (int i = 0; i < list.Length; i++)
            {
                items.Add(Serializer.Instance.Deserialize(currentType!, list[i], deep - 1));
            }
            return items;
        }

        public string Serialize(Type type, object value, int deep)
        {
            return string.Join((char)deep, Read()
               .Select(x => Serializer.Instance.Serialize(x.GetType(), x, deep - 1)));

            IEnumerable<object> Read()
            {
                foreach (var item in (IEnumerable)value)
                    yield return item;
            }
        }
    }
}
