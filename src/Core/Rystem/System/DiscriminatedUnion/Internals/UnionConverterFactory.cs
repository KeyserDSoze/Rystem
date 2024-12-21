namespace System.Text.Json.Serialization
{
    internal sealed class UnionConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
            {
                return false;
            }
            if (typeToConvert.GetGenericTypeDefinition() == typeof(AnyOf<,>)
                || typeToConvert.GetGenericTypeDefinition() == typeof(AnyOf<,,>)
                || typeToConvert.GetGenericTypeDefinition() == typeof(AnyOf<,,,>))
            {
                return true;
            }
            return false;
        }
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Type[] typeArguments = typeToConvert.GetGenericArguments();
            return new UnionConverterEngine(options, typeArguments);
        }
    }
}
