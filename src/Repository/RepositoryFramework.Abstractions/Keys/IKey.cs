using System.Reflection;
using System.Text.Json;

namespace RepositoryFramework
{
    public sealed class KeySettings<TKey>
        where TKey : notnull
    {
        private enum CurrentType
        {
            Primitive,
            String,
            Guid,
            DateTime,
            DateTimeOffset,
            TimeSpan,
            Nint,
            Nuint,
            Jsonable
        }
        public bool IsJsonable => _type == CurrentType.Jsonable;
        private readonly CurrentType _type = Calculate();
        private static CurrentType Calculate()
        {
            var type = typeof(TKey);
            if (type == typeof(string))
                return CurrentType.String;
            else if (type == typeof(Guid))
                return CurrentType.Guid;
            else if (type == typeof(DateTime))
                return CurrentType.DateTime;
            else if (type == typeof(DateTimeOffset))
                return CurrentType.DateTimeOffset;
            else if (type == typeof(TimeSpan))
                return CurrentType.TimeSpan;
            else if (type == typeof(nint))
                return CurrentType.Nint;
            else if (type == typeof(nuint))
                return CurrentType.Nuint;
            else
            {
                var hasProperties = type.FetchProperties().Length > 0;
                if (hasProperties)
                    return CurrentType.Jsonable;
                else
                    return CurrentType.Primitive;
            }
        }
        public string AsString(TKey key)
        {
            if (IsJsonable)
                return key.ToJson();
            else if (key is IKey iKey)
                return iKey.AsString();
            else
                return key.ToString()!;
        }
        public TKey Parse(string key)
        {
            if (_type == CurrentType.String)
                return (dynamic)key;
            else if (_type == CurrentType.Guid)
                return (dynamic)Guid.Parse(key);
            else if (_type == CurrentType.DateTime)
                return (dynamic)DateTime.Parse(key);
            else if (_type == CurrentType.DateTimeOffset)
                return (dynamic)DateTimeOffset.Parse(key);
            else if (_type == CurrentType.TimeSpan)
                return (dynamic)TimeSpan.Parse(key);
            else if (_type == CurrentType.Nint)
                return (dynamic)nint.Parse(key);
            else if (_type == CurrentType.Nuint)
                return (dynamic)nuint.Parse(key);
            else
            {
                if (IsJsonable)
                    return key.FromJson<TKey>();
                else
                    return (TKey)Convert.ChangeType(key, typeof(TKey));
            }
        }
    }
    public interface IKey
    {
        string AsString();
        abstract static IKey Parse(string keyAsString);
        internal static string GetStringedValues(params object[] inputs)
            => string.Join('-', inputs.Select(x => x.ToString()));
    }
}
