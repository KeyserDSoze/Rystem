using System.Reflection;

namespace System.Text.Json
{
    internal sealed class ReadHelper<T> : ReadHelper
    {
        private readonly ReadDelegate _readDelegate;
        private delegate T ReadDelegate(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);
        public ReadHelper(object converter, MethodInfo methodInfo)
        {
            _readDelegate = (Delegate.CreateDelegate(typeof(ReadDelegate), converter, methodInfo) as ReadDelegate)!;
        }
        public override object Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
            => _readDelegate.Invoke(ref reader, type, options)!;
    }
}
