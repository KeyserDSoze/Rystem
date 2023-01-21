using System.Reflection;
using System.Text.Json;

namespace System
{
    public static class CopyExtensions
    {
        public static T? ToDeepCopy<T>(this T? source)
        {
            if (source == null)
                return default;
            else
                return source.ToJson().FromJson<T>();
        }
        public static object? ToDeepCopy(this object? source)
        {
            if (source == null)
                return default;
            else
                return source.ToJson().FromJson(source.GetType());
        }
        public static void CopyPropertiesFrom(this object destination, object? source)
        {
            Type type = destination.GetType();
            if (source == null)
                source = type.CreateWithDefaultConstructorPropertiesAndField();
            foreach (var property in type.FetchProperties())
            {
                if (property.SetMethod != null)
                    property.SetValue(destination, property.GetValue(source));
            }
        }
        public static void CopyPropertiesFrom<T>(this T? destination, T? source)
        {
            if (source == null)
                source = typeof(T).CreateWithDefaultConstructorPropertiesAndField<T>();
            if (destination == null)
                destination = typeof(T).CreateWithDefaultConstructorPropertiesAndField<T>();
            foreach (var property in typeof(T).FetchProperties())
            {
                if (property.SetMethod != null)
                    property.SetValue(destination, property.GetValue(source));
            }
        }
    }
}
