using System.Text.Json;

namespace System.Text
{
    public static class Base64Extensions
    {
        public static string ToBase64(this string value) 
            => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        public static string ToBase64<T>(this T entity) 
            => entity.ToJson().ToBase64();
        public static string FromBase64(this string encodedValue) 
            => Encoding.UTF8.GetString(Convert.FromBase64String(encodedValue));
        public static T FromBase64<T>(this string encodedValue) 
            => encodedValue.FromBase64().FromJson<T>();
    }
}