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
        return new TypeDescriptor
        {
            CSharpName = type.Name,
            FullName = type.FullName ?? type.Name,
            TypeScriptName = type.Name,
            IsPrimitive = false,
            IsNullable = isNullable || !type.IsValueType, // Reference types are nullable by default
            IsArray = false,
            IsDictionary = false,
            IsEnum = false,
            ClrType = type
        };
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
