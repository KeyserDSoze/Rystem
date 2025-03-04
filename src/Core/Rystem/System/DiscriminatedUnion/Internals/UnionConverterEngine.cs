﻿using System.Reflection;
using System.Text.RegularExpressions;

namespace System.Text.Json.Serialization
{
    internal sealed class UnionConverterEngine : JsonConverter<object>
    {
        private const string ReadMethodName = "Read";
        private const string WriteMethodName = "Write";
        private readonly List<Type> _primitiveTypes;
        private readonly List<Type> _objectTypes;
        private readonly Dictionary<Type, ReadHelper> _readers = [];
        private readonly Dictionary<Type, WriteHelper> _writers = [];
        private readonly Dictionary<Type, Dictionary<string, bool>> _properties = [];
        private readonly Dictionary<string, List<SelectorHelper>> _selectors = [];
        private readonly Type _anyOfType;

        private sealed class SelectorHelper
        {
            public required Func<object?, Type?> Check { get; init; }
        }
        private readonly Type? _defaultType;
        public UnionConverterEngine(JsonSerializerOptions options, params Type[] types)
        {
            _primitiveTypes = [.. types.Where(x => x.IsPrimitive())];
            _objectTypes = [.. types.Where(x => !x.IsPrimitive())];
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
                var isDefault = type.GetCustomAttribute<AnyOfJsonDefaultAttribute>() != null;
                if (isDefault)
                    _defaultType = type;
                var selectorsForClass = new List<AnyOfJsonSelectorAttribute>();
                var anyOfJsonClassSelectorAttributes = type.GetCustomAttributes<AnyOfJsonClassSelectorAttribute>();
                if (anyOfJsonClassSelectorAttributes != null)
                    selectorsForClass.AddRange(anyOfJsonClassSelectorAttributes);
                var anyOfJsonRegexClassSelectorAttribute = type.GetCustomAttributes<AnyOfJsonRegexClassSelectorAttribute>();
                if (anyOfJsonRegexClassSelectorAttribute != null)
                    selectorsForClass.AddRange(anyOfJsonRegexClassSelectorAttribute);
                foreach (var property in type.GetProperties().Where(x => x.GetCustomAttribute<JsonIgnoreAttribute>() == null))
                {
                    var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
                    _properties[type].Add(name, true);
                    var selector = selectorsForClass.FirstOrDefault(x => x.PropertyName == name) ?? property.GetCustomAttribute<AnyOfJsonRegexSelectorAttribute>() ?? property.GetCustomAttribute<AnyOfJsonSelectorAttribute>();
                    if (selector != null)
                    {
                        _selectors.TryAdd(name, []);
                        var regexSelectors = new List<Regex>();
                        if (selector.IsRegex)
                        {
                            foreach (var value in selector.Values)
                            {
                                regexSelectors.Add(new Regex(value.ToString()!, RegexOptions.Compiled));
                            }
                        }
                        _selectors[name].Add(new SelectorHelper
                        {
                            Check = (value) =>
                            {
                                if (selector.IsRegex)
                                {
                                    var castedValue = value?.ToString() ?? string.Empty;
                                    if (selector.IsRegex && regexSelectors.Any(t => t.IsMatch(castedValue)))
                                        return type;
                                }
                                else
                                {
                                    var castedValue = value?.Cast(property.PropertyType);
                                    if (selector.Values.Any(t => (t == null && castedValue == null) || (t != null && t.Equals(castedValue))))
                                        return type;
                                }
                                return null;
                            }
                        });
                    }
                }
            }
            _anyOfType = GetUnionType(types.Length).MakeGenericType(types);
            static Type GetUnionType(int numberOfTypes)
            {
                return numberOfTypes switch
                {
                    2 => typeof(AnyOf<,>),
                    3 => typeof(AnyOf<,,>),
                    4 => typeof(AnyOf<,,,>),
                    5 => typeof(AnyOf<,,,,>),
                    6 => typeof(AnyOf<,,,,,>),
                    7 => typeof(AnyOf<,,,,,,>),
                    8 => typeof(AnyOf<,,,,,,,>),
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
            var currentType = GetPossibleType(reader) ?? _defaultType;
            if (currentType != null)
            {
                var readHelper = _readers[currentType];
                var result = readHelper.Read(ref reader, currentType, options);
                var instance = (dynamic)Activator.CreateInstance(_anyOfType, [result])!;
                return instance;
            }
            else
            {
                reader.Skip();
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
                return GetPossiblePrimitiveType(reader);
            }
            else
            {
                return GetPossibleNonPrimitiveType(reader);
            }
        }
        private Type? GetPossiblePrimitiveType(Utf8JsonReader reader)
        {
            foreach (var type in _primitiveTypes)
            {
                var isPrimitive =
                       (reader.TokenType == JsonTokenType.String && type == typeof(string))
                    || (reader.TokenType == JsonTokenType.Number && type.IsNumeric())
                    || ((reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False) && (type == typeof(bool) || type == typeof(bool?)));
                if (isPrimitive)
                    return type;
            }
            return null;
        }
        private Type? GetPossibleNonPrimitiveType(Utf8JsonReader reader)
        {
            List<string> properties = [];
            var initialDepth = reader.CurrentDepth;
            var availableTypes = _objectTypes;
            while (reader.Read())
            {
                if (initialDepth == reader.CurrentDepth && reader.TokenType == JsonTokenType.EndObject)
                    break;
                else if (initialDepth + 1 == reader.CurrentDepth && reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString()!;
                    properties.Add(propertyName);
                    if (_selectors.Count > 0 && _selectors.TryGetValue(propertyName, out var values))
                    {
                        var newAvailable = new List<Type>();
                        var value = ReadValue(reader);
                        foreach (var val in values)
                        {
                            var possibleType = val.Check.Invoke(value);
                            if (possibleType != null)
                                newAvailable.Add(possibleType);
                        }
                        if (newAvailable.Count == 1)
                            return newAvailable[0];
                        else
                            availableTypes = [.. newAvailable.Where(x => availableTypes.Contains(x))];
                    }
                }
            }
            foreach (var type in availableTypes)
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
            return null;
        }
        private static object? ReadValue(Utf8JsonReader reader)
        {
            _ = reader.Read();
            object? value = null;
            if (reader.TokenType == JsonTokenType.True)
                value = true;
            else if (reader.TokenType == JsonTokenType.False)
                value = false;
            else if (reader.TokenType == JsonTokenType.String)
            {
                value = reader.GetString();
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                value = reader.GetDecimal();
            }
            return value;
        }
        public override void Write(
            Utf8JsonWriter writer,
            object value,
            JsonSerializerOptions options)
        {
            if (value is IAnyOf anyOf)
            {
                var currentType = anyOf.GetCurrentType();
                if (currentType != null)
                {
                    var currentValue = anyOf.Value;
                    if (currentValue != null)
                    {
                        var writeHelper = _writers[currentType];
                        writeHelper.Write(writer, currentValue, options);
                    }
                }
            }
        }
    }
}
