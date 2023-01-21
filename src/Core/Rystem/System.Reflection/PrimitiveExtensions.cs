namespace System.Reflection
{
    public static class PrimitiveExtensions
    {
        private static readonly Type[] PrimitiveTypes = new Type[] {
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
            => type.IsPrimitive || PrimitiveTypes.Contains(type) || type.IsEnum || Convert.GetTypeCode(type) != TypeCode.Object ||
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
    }
}