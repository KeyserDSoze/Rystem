using System.Reflection;
using System.Text.Json;

namespace RepositoryFramework
{
    public interface IKey
    {
        string AsString();
        internal static string GetStringedValues(params object[] inputs)
            => string.Join('-', inputs.Select(x => x.ToString()));
        public static bool IsJsonable(Type keyType)
        {
            if (keyType == typeof(string) || keyType == typeof(Guid) ||
                keyType == typeof(DateTimeOffset) || keyType == typeof(TimeSpan) ||
                keyType == typeof(nint) || keyType == typeof(nuint))
                return false;
            else
                return keyType.FetchProperties().Length > 0;
        }
        public static string AsString<TKey>(TKey key)
            where TKey : notnull
        {
            if (IsJsonable(typeof(TKey)))
                return key.ToJson();
            else if (key is IKey iKey)
                return iKey.AsString();
            else
                return key.ToString()!;
        }
        public static Func<string, TKey> Parser<TKey>()
        {
            var type = typeof(TKey);
            if (type == typeof(string))
                return key => (dynamic)key;
            else if (type == typeof(Guid))
                return key => (dynamic)Guid.Parse(key);
            else if (type == typeof(DateTimeOffset))
                return key => (dynamic)DateTimeOffset.Parse(key);
            else if (type == typeof(TimeSpan))
                return key => (dynamic)TimeSpan.Parse(key);
            else if (type == typeof(nint))
                return key => (dynamic)nint.Parse(key);
            else if (type == typeof(nuint))
                return key => (dynamic)nuint.Parse(key);
            else
            {
                var hasProperties = type.FetchProperties().Length > 0;
                if (hasProperties)
                    return key => key.FromJson<TKey>();
                else
                    return key => (TKey)Convert.ChangeType(key, type);
            }
        }
    }
}
