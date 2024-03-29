﻿namespace System.Reflection
{
    public static class PrimitiveExtensions
    {
        private static readonly Type[] s_primitiveTypes = new Type[] {
            typeof(string),
            typeof(decimal),
            typeof(DateTime),
            typeof(DateOnly),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid)
        };
        public static bool IsPrimitive<T>(this T? entity)
            => entity?.GetType().IsPrimitive() ?? typeof(T).IsPrimitive();
        public static bool IsPrimitive(this Type type)
            => type.IsPrimitive || s_primitiveTypes.Contains(type) || type.IsEnum || Convert.GetTypeCode(type) != TypeCode.Object ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsPrimitive(type.GetGenericArguments()[0]));
        public static bool IsNumeric<T>(this T? entity)
            => entity?.GetType().IsNumeric() ?? typeof(T).IsNumeric();
        public static bool IsNumeric(this Type type)
            => type == typeof(int) || type == typeof(int?) || type == typeof(uint) || type == typeof(uint?)
                || type == typeof(short) || type == typeof(short?) || type == typeof(ushort) || type == typeof(ushort?)
                || type == typeof(long) || type == typeof(long?) || type == typeof(ulong) || type == typeof(ulong?)
                || type == typeof(nint) || type == typeof(nint?) || type == typeof(nuint) || type == typeof(nuint?)
                || type == typeof(float) || type == typeof(float?) || type == typeof(double) || type == typeof(double?)
                || type == typeof(decimal) || type == typeof(decimal?);
        public static bool IsBoolean(this Type type)
            => type == typeof(bool) || type == typeof(bool?);
        public static bool IsDateTime(this Type type)
            => type == typeof(DateTime) || type == typeof(DateTime?)
                || type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?);
        public static bool IsEnumerable(this Type type)
            => type.GetInterfaces().Any(x => x.Name.StartsWith("IEnumerable"));
        public static bool IsDictionary(this Type type)
            => type.GetInterfaces().Any(x => x.Name.StartsWith("IDictionary"));
    }
}
