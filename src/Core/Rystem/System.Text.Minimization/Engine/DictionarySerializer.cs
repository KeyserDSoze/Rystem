using System.Collections;

namespace System.Text.Minimization
{
    internal class DictionarySerializer : IMinimizationInterpreter
    {
        public int Priority => 5;
        public bool IsValid(Type type)
        {
            if (!type.IsArray)
            {
                var interfaces = type.GetInterfaces();
                if (type.Name.Contains("IDictionary`2") || interfaces.Any(x => x.Name.Contains("IDictionary`2")))
                    return true;
            }
            return false;
        }
        public dynamic Deserialize(Type type, string value, int deep = int.MaxValue)
        {
            IDictionary dictionary = (Activator.CreateInstance(type) as IDictionary)!;
            var list = value.Split((char)(deep));
            var keyType = type.GetGenericArguments().First();
            var valueType = type.GetGenericArguments().Last();
            for (int i = 0; i < list.Length; i++)
            {
                var values = list[i].Split((char)(deep - 1));
                dictionary.Add(Serializer.Instance.Deserialize(keyType!, values.First(), deep - 2),
                    Serializer.Instance.Deserialize(valueType!, values.Last(), deep - 2));
            }
            return dictionary;
        }

        public string Serialize(Type type, object value, int deep)
        {
            return string.Join((char)deep, Read()
                .Select(x => $"{Serializer.Instance.Serialize(x.Key.GetType(), x.Key, deep - 2)}{(char)(deep - 1)}{Serializer.Instance.Serialize(x.Value.GetType(), x.Value, deep - 2)}"));

            IEnumerable<(object Key, object Value)> Read()
            {
                foreach (dynamic item in (IDictionary)value)
                    yield return (item.Key, item.Value);
            }
        }
    }
}
