using System.Reflection;

namespace System.Text.Json
{
    internal sealed class WriteHelper
    {
        private readonly object _converter;
        private readonly MethodInfo _methodInfo;

        public WriteHelper(object converter, MethodInfo methodInfo)
        {
            _converter = converter;
            _methodInfo = methodInfo;
        }
        public void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            _methodInfo.Invoke(_converter, [writer, value, options]);
        }
    }
}
