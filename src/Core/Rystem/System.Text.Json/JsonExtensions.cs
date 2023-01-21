using System.Reflection;

namespace System.Text.Json
{
    public static class JsonExtensions
    {
        public static string ToJson<T>(this T entity, JsonSerializerOptions? options = default)
            => JsonSerializer.Serialize(entity, options);
        private static readonly MethodInfo s_fromJsonMethod = typeof(JsonExtensions).GetMethods().First(x => x.Name == nameof(FromJson) && x.GetParameters().First().ParameterType == typeof(string));
        public static T FromJson<T>(this string entity, JsonSerializerOptions? options = default)
            => JsonSerializer.Deserialize<T>(entity, options)!;
        public static T FromJson<T>(this byte[] entity, JsonSerializerOptions? options = default)
            => entity.ConvertToString().FromJson<T>(options);
        public static object? FromJson(this string entity, Type type, JsonSerializerOptions? options = default)
            => s_fromJsonMethod.MakeGenericMethod(type).Invoke(null, new object[2] { entity, options! });
        public static async Task<T> FromJsonAsync<T>(this Stream entity, JsonSerializerOptions? options = default)
            => (await entity.ConvertToStringAsync().NoContext()).FromJson<T>(options);
    }
}