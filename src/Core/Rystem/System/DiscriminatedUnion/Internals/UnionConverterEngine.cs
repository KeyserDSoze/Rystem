using System.Reflection;

namespace System.Text.Json.Serialization
{
    internal sealed class UnionConverterEngine : JsonConverter<object>
    {
        private const string ReadMethodName = "Read";
        private const string WriteMethodName = "Write";
        private readonly Type[] _types;
        private readonly Dictionary<Type, ReadHelper> _readers = [];
        private readonly Dictionary<Type, WriteHelper> _writers = [];
        private readonly Dictionary<Type, Dictionary<string, bool>> _properties = [];
        private readonly Type _unionOfType;
        public UnionConverterEngine(JsonSerializerOptions options, params Type[] types)
        {
            _types = types;
            foreach (var type in types)
            {
                var converter = options.GetConverter(type);
                var readHelperType = typeof(ReadHelper<>).MakeGenericType(type);
                var jsonConvertType = typeof(JsonConverter<>).MakeGenericType(type!);
                var readMethod = jsonConvertType.GetMethods().First(x => x.Name == ReadMethodName);
                var writeMethod = jsonConvertType.GetMethods().First(x => x.Name == WriteMethodName);
                var reader = (Activator.CreateInstance(readHelperType, [converter, readMethod]) as ReadHelper)!;
                _readers.Add(type, reader);
                _writers.Add(type, new WriteHelper(converter, writeMethod));
                _properties.Add(type, []);
                foreach (var property in type.GetProperties())
                {
                    var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
                    _properties[type].Add(name, true);
                }
            }
            _unionOfType = GetUnionType(types.Length).MakeGenericType(_types);

            static Type GetUnionType(int numberOfTypes)
            {
                return numberOfTypes switch
                {
                    2 => typeof(UnionOf<,>),
                    3 => typeof(UnionOf<,,>),
                    4 => typeof(UnionOf<,,,>),
                    _ => throw new NotSupportedException()
                };
            }
        }
        public override object Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null!;
            }
            var currentType = GetPossibleType(reader);
            if (currentType != null)
            {
                var readHelper = _readers[currentType];
                var result = readHelper.Read(ref reader, currentType, options);
                var instance = (dynamic)Activator.CreateInstance(_unionOfType, [result])!;
                return instance;
            }
            else
            {
                return null!;
            }
        }
        private Type? GetPossibleType(Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String ||
                reader.TokenType == JsonTokenType.Number ||
                reader.TokenType == JsonTokenType.False ||
                reader.TokenType == JsonTokenType.True)
            {
                foreach (var type in _types.Where(x => x.IsPrimitive()))
                {
                    var isPrimitive =
                           (reader.TokenType == JsonTokenType.String && type == typeof(string))
                        || (reader.TokenType == JsonTokenType.Number && type.IsNumeric())
                        || ((reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False) && (type == typeof(bool) || type == typeof(bool?)));
                    if (isPrimitive)
                        return type;
                }
            }
            else
            {
                List<string> properties = [];
                var initialDepth = reader.CurrentDepth;
                while (reader.Read())
                {
                    if (initialDepth == reader.CurrentDepth && reader.TokenType == JsonTokenType.EndObject)
                        break;
                    else if (initialDepth + 1 == reader.CurrentDepth && reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var name = reader.GetString()!;
                        properties.Add(name);
                    }
                }
                foreach (var type in _types.Where(x => !x.IsPrimitive()))
                {
                    var propertiesAsNameMap = _properties[type];
                    if (propertiesAsNameMap.Count >= properties.Count)
                    {
                        var correctType = true;
                        foreach (var property in properties)
                        {
                            if (!propertiesAsNameMap.ContainsKey(property))
                            {
                                correctType = false;
                                break;
                            }
                        }
                        if (correctType)
                            return type;
                    }
                }
            }
            return null;
        }
        public override void Write(
            Utf8JsonWriter writer,
            object value,
            JsonSerializerOptions options)
        {
            if (value is IUnionOf unionOf)
            {
                var currentValue = unionOf.Value;
                if (currentValue != null)
                {
                    var currentType = currentValue.GetType();
                    var writeHelper = _writers[currentType];
                    writeHelper.Write(writer, currentValue, options);
                }
            }
        }
    }
}
