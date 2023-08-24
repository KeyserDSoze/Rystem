using System.Reflection;
using System.Text;

namespace RepositoryFramework
{
    public static class DefaultKeyExtensions
    {
        public static string AsString<T>(this T key)
            where T : IDefaultKey
        {
            var stringBuilder = new StringBuilder();
            foreach (var property in key.GetType().FetchProperties())
            {
                var value = property.GetValue(key)?.ToString() ?? string.Empty;
                if (stringBuilder.Length == 0)
                    stringBuilder.Append(value);
                else
                    stringBuilder.Append($"{IDefaultKey.DefaultSeparator}{value}");
            }
            return stringBuilder.ToString();
        }
        public static T Parse<T>(string keyAsString)
        {
            var splitted = keyAsString.Split(IDefaultKey.DefaultSeparator);
            var defaultInstance = Activator.CreateInstance<T>();
            var counter = 0;
            foreach (var property in typeof(T).FetchProperties())
            {
                property.SetValue(defaultInstance, splitted[counter].Cast(property.PropertyType));
                counter++;
            }
            return defaultInstance;
        }
    }
}
