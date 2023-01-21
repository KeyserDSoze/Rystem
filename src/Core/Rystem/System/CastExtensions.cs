using System.Reflection;

namespace System
{
    public static class CastExtensions
    {
        public static T? Cast<T>(this object? entity)
        {
            if (entity == null)
                return default;
            if (entity is T casted)
                return casted;
            else if (entity is string stringEntity)
            {
                var type = typeof(T);
                if (type == typeof(string))
                    return (dynamic)stringEntity;
                else if (type == typeof(Guid))
                    return (dynamic)Guid.Parse(stringEntity);
                else if (type == typeof(DateTimeOffset))
                    return (dynamic)DateTimeOffset.Parse(stringEntity);
                else if (type == typeof(TimeSpan))
                    return (dynamic)TimeSpan.Parse(stringEntity);
                else if (type == typeof(nint))
                    return (dynamic)nint.Parse(stringEntity);
                else if (type == typeof(nuint))
                    return (dynamic)nuint.Parse(stringEntity);
            }
            if (entity is IConvertible)
                return (T)Convert.ChangeType(entity, typeof(T));
            else
                return (T)entity;
        }
        public static dynamic Cast(this object? entity, Type typeToCast)
            => Generics.WithStatic(typeof(CastExtensions), nameof(Cast), typeToCast)
                .Invoke(entity!);
    }
}
