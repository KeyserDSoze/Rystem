using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Rules;

namespace RepositoryFramework.Tools.TypescriptGenerator.Analysis;

/// <summary>
/// Resolves C# types to TypeScript type descriptors.
/// </summary>
public class TypeResolver
{
    private readonly Dictionary<Type, TypeDescriptor> _cache = [];

    /// <summary>
    /// Resolves a C# Type to a TypeDescriptor.
    /// </summary>
    public TypeDescriptor Resolve(Type type)
    {
        // Check cache first
        if (_cache.TryGetValue(type, out var cached))
            return cached;

        var descriptor = ResolveInternal(type);
        _cache[type] = descriptor;
        return descriptor;
    }

    private TypeDescriptor ResolveInternal(Type type)
    {
        // Handle nullable value types
        var underlyingNullable = Nullable.GetUnderlyingType(type);
        var isNullable = underlyingNullable != null;
        var actualType = underlyingNullable ?? type;

        // Handle nullable reference types (check for NullableAttribute would be needed for full support)
        // For now, we treat reference types as potentially nullable

        // Check if it's an enum
        if (actualType.IsEnum)
        {
            return CreateEnumDescriptor(actualType, isNullable);
        }

        // Check if it's a primitive type
        var primitiveTs = PrimitiveTypeRules.GetTypeScriptType(actualType);
        if (primitiveTs != null)
        {
            return CreatePrimitiveDescriptor(actualType, primitiveTs, isNullable);
        }

        // Check if it's a type with [JsonConverter] that wraps a single primitive value
        // (e.g., LocalizedFormatString with JsonConverter that serializes as plain string)
        var jsonConverterPrimitive = GetJsonConverterPrimitiveType(actualType);
        if (jsonConverterPrimitive != null)
        {
            return CreatePrimitiveDescriptor(actualType, jsonConverterPrimitive, isNullable);
        }

        // Check if it's a dictionary
        if (IsDictionary(actualType, out var keyType, out var valueType))
        {
            return CreateDictionaryDescriptor(actualType, keyType!, valueType!, isNullable);
        }

        // Check if it's an array or collection
        if (IsCollection(actualType, out var elementType))
        {
            return CreateArrayDescriptor(actualType, elementType!, isNullable);
        }

        // It's a complex type (class, record, struct)
        return CreateComplexDescriptor(actualType, isNullable);
    }

    private static TypeDescriptor CreatePrimitiveDescriptor(Type type, string tsType, bool isNullable)
    {
        return new TypeDescriptor
        {
            CSharpName = type.Name,
            FullName = type.FullName ?? type.Name,
            TypeScriptName = tsType,
            IsPrimitive = true,
            IsNullable = isNullable,
            IsArray = false,
            IsDictionary = false,
            IsEnum = false,
            ClrType = type
        };
    }

    private static TypeDescriptor CreateEnumDescriptor(Type type, bool isNullable)
    {
        return new TypeDescriptor
        {
            CSharpName = type.Name,
            FullName = type.FullName ?? type.Name,
            TypeScriptName = type.Name,
            IsPrimitive = false,
            IsNullable = isNullable,
            IsArray = false,
            IsDictionary = false,
            IsEnum = true,
            ClrType = type
        };
    }

    private TypeDescriptor CreateArrayDescriptor(Type type, Type elementType, bool isNullable)
    {
        var elementDescriptor = Resolve(elementType);

        return new TypeDescriptor
        {
            CSharpName = type.Name,
            FullName = type.FullName ?? type.Name,
            TypeScriptName = $"{elementDescriptor.TypeScriptName}[]",
            IsPrimitive = false,
            IsNullable = isNullable,
            IsArray = true,
            IsDictionary = false,
            IsEnum = false,
            ElementType = elementDescriptor,
            ClrType = type
        };
    }

    private TypeDescriptor CreateDictionaryDescriptor(Type type, Type keyType, Type valueType, bool isNullable)
    {
        var keyDescriptor = Resolve(keyType);
        var valueDescriptor = Resolve(valueType);

        return new TypeDescriptor
        {
            CSharpName = type.Name,
            FullName = type.FullName ?? type.Name,
            TypeScriptName = $"Record<{keyDescriptor.TypeScriptName}, {valueDescriptor.TypeScriptName}>",
            IsPrimitive = false,
            IsNullable = isNullable,
            IsArray = false,
            IsDictionary = true,
            IsEnum = false,
            KeyType = keyDescriptor,
            ValueType = valueDescriptor,
            ClrType = type
        };
    }

    private static TypeDescriptor CreateComplexDescriptor(Type type, bool isNullable)
    {
        // Handle generic type parameters (T, TKey, etc.)
        if (type.IsGenericParameter)
        {
            return new TypeDescriptor
            {
                CSharpName = type.Name,
                FullName = type.Name,
                TypeScriptName = type.Name, // T stays as T in TypeScript
                IsPrimitive = false,
                IsNullable = false, // Generic parameters handle nullability via usage
                IsArray = false,
                IsDictionary = false,
                IsEnum = false,
                ClrType = type
            };
        }

        // Handle closed generic types (e.g., EntityVersions<Timeline>)
        var typeName = type.Name;
        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            // Construct TypeScript generic syntax: EntityVersions<Timeline>
            var baseName = typeName.Contains('`') ? typeName[..typeName.IndexOf('`')] : typeName;
            var typeArgs = type.GetGenericArguments();
            var argNames = string.Join(", ", typeArgs.Select(t => 
                t.IsGenericParameter ? t.Name : GetCleanTypeName(t)));
            typeName = $"{baseName}<{argNames}>";
        }
        else if (type.IsGenericTypeDefinition)
        {
            // Open generic: EntityVersions`1 -> EntityVersions<T>
            var baseName = typeName.Contains('`') ? typeName[..typeName.IndexOf('`')] : typeName;
            var typeParams = type.GetGenericArguments();
            var paramNames = string.Join(", ", typeParams.Select(p => p.Name));
            typeName = $"{baseName}<{paramNames}>";
        }

        return new TypeDescriptor
        {
            CSharpName = type.Name,
            FullName = type.FullName ?? type.Name,
            TypeScriptName = typeName,
            IsPrimitive = false,
            IsNullable = isNullable || !type.IsValueType, // Reference types are nullable by default
            IsArray = false,
            IsDictionary = false,
            IsEnum = false,
            ClrType = type
        };
    }

    /// <summary>
    /// Determines if a type with [JsonConverter] is serialized as a primitive JSON value.
    /// Uses two strategies:
    /// 1. IL analysis of the converter's Write method to detect calls to Utf8JsonWriter.Write*Value
    /// 2. Fallback heuristic: single primitive property pattern
    /// Returns the TypeScript type name ("string", "number", "boolean") or null if not primitive.
    /// </summary>
    private static string? GetJsonConverterPrimitiveType(Type type)
    {
        var converterAttr = type.GetCustomAttribute<JsonConverterAttribute>();
        if (converterAttr?.ConverterType == null)
            return null;

        // Strategy 1: Analyze the converter's Write method IL to see what it writes
        var fromIL = TryDetectPrimitiveFromConverterIL(converterAttr.ConverterType, type);
        if (fromIL != null)
            return fromIL;

        // Strategy 2: Fallback — single primitive property heuristic
        return TryDetectPrimitiveFromSingleProperty(type);
    }

    /// <summary>
    /// Inspects the IL of the converter's Write method to determine what Utf8JsonWriter
    /// methods it calls. If it calls exactly one Write*Value method (and no WriteStartObject
    /// or WriteStartArray), the output is that primitive type.
    /// Works for ANY converter regardless of the class structure.
    /// </summary>
    private static string? TryDetectPrimitiveFromConverterIL(Type converterType, Type targetType)
    {
        try
        {
            var writeMethod = converterType.GetMethod("Write",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [typeof(Utf8JsonWriter), targetType, typeof(JsonSerializerOptions)],
                null);

            if (writeMethod == null)
                return null;

            var body = writeMethod.GetMethodBody();
            var il = body?.GetILAsByteArray();
            if (il == null)
                return null;

            var module = converterType.Module;
            var writesString = false;
            var writesNumber = false;
            var writesBool = false;

            for (var i = 0; i < il.Length - 4; i++)
            {
                // call (0x28) or callvirt (0x6F) followed by 4-byte metadata token
                if (il[i] is not (0x28 or 0x6F))
                    continue;

                var token = il[i + 1] | (il[i + 2] << 8) | (il[i + 3] << 16) | (il[i + 4] << 24);
                try
                {
                    var method = module.ResolveMethod(token);
                    if (method?.DeclaringType != typeof(Utf8JsonWriter))
                        continue;

                    switch (method.Name)
                    {
                        case "WriteStringValue":
                            writesString = true;
                            break;
                        case "WriteNumberValue":
                            writesNumber = true;
                            break;
                        case "WriteBooleanValue":
                            writesBool = true;
                            break;
                        case "WriteStartObject" or "WriteStartArray":
                            // Converter produces a complex JSON structure, not a primitive
                            return null;
                    }
                }
                catch
                {
                    // Token resolution failed (cross-assembly, generic context, etc.) — skip
                }

                i += 4; // Skip the 4-byte token
            }

            // If exactly one primitive write type detected, return it
            var count = (writesString ? 1 : 0) + (writesNumber ? 1 : 0) + (writesBool ? 1 : 0);
            if (count == 1)
            {
                if (writesString) return "string";
                if (writesNumber) return "number";
                if (writesBool) return "boolean";
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Fallback heuristic: if the type has exactly one public non-indexer property
    /// of a primitive type, assume the converter serializes as that primitive.
    /// </summary>
    private static string? TryDetectPrimitiveFromSingleProperty(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0 && p.CanRead)
            .ToArray();

        if (properties.Length != 1)
            return null;

        var propType = properties[0].PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;
        return PrimitiveTypeRules.GetTypeScriptType(underlyingType);
    }

    /// <summary>
    /// Gets a clean type name for generic arguments.
    /// </summary>
    private static string GetCleanTypeName(Type type)
    {
        if (type.IsGenericParameter)
            return type.Name;

        if (type.IsGenericType)
        {
            var baseName = type.Name.Contains('`') ? type.Name[..type.Name.IndexOf('`')] : type.Name;
            var typeArgs = type.GetGenericArguments();
            var argNames = string.Join(", ", typeArgs.Select(GetCleanTypeName));
            return $"{baseName}<{argNames}>";
        }

        return type.Name;
    }

    /// <summary>
    /// Checks if a type is a collection (array, List, IEnumerable, etc.).
    /// </summary>
    private static bool IsCollection(Type type, out Type? elementType)
    {
        elementType = null;

        // Arrays
        if (type.IsArray)
        {
            elementType = type.GetElementType();
            return true;
        }

        // IEnumerable<T>, List<T>, etc.
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();

            // Check for common collection types
            if (genericDef == typeof(List<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(IReadOnlyList<>) ||
                genericDef == typeof(IReadOnlyCollection<>) ||
                genericDef == typeof(HashSet<>) ||
                genericDef == typeof(ISet<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
        }

        // Check interfaces for IEnumerable<T>
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                // Make sure it's not a dictionary or string
                if (!IsDictionary(type, out _, out _) && type != typeof(string))
                {
                    elementType = iface.GetGenericArguments()[0];
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a type is a dictionary (Dictionary, IDictionary, etc.).
    /// </summary>
    private static bool IsDictionary(Type type, out Type? keyType, out Type? valueType)
    {
        keyType = null;
        valueType = null;

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();

            if (genericDef == typeof(Dictionary<,>) ||
                genericDef == typeof(IDictionary<,>) ||
                genericDef == typeof(IReadOnlyDictionary<,>))
            {
                var args = type.GetGenericArguments();
                keyType = args[0];
                valueType = args[1];
                return true;
            }
        }

        // Check interfaces
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType)
            {
                var genericDef = iface.GetGenericTypeDefinition();
                if (genericDef == typeof(IDictionary<,>) ||
                    genericDef == typeof(IReadOnlyDictionary<,>))
                {
                    var args = iface.GetGenericArguments();
                    keyType = args[0];
                    valueType = args[1];
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Clears the type cache.
    /// </summary>
    public void ClearCache() => _cache.Clear();
}
