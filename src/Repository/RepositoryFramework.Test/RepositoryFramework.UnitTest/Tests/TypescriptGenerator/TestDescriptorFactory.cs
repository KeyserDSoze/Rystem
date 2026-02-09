using System.Linq;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Factory methods for creating test descriptors with required properties.
/// </summary>
internal static class TestDescriptorFactory
{
    // Primitive TypeDescriptors
    public static TypeDescriptor StringType => new()
    {
        CSharpName = "String",
        FullName = "System.String",
        TypeScriptName = "string",
        IsPrimitive = true,
        IsNullable = false,
        IsArray = false,
        IsDictionary = false,
        IsEnum = false
    };

    public static TypeDescriptor NumberType => new()
    {
        CSharpName = "Int32",
        FullName = "System.Int32",
        TypeScriptName = "number",
        IsPrimitive = true,
        IsNullable = false,
        IsArray = false,
        IsDictionary = false,
        IsEnum = false
    };

    public static TypeDescriptor BooleanType => new()
    {
        CSharpName = "Boolean",
        FullName = "System.Boolean",
        TypeScriptName = "boolean",
        IsPrimitive = true,
        IsNullable = false,
        IsArray = false,
        IsDictionary = false,
        IsEnum = false
    };

    public static TypeDescriptor CreateArrayType(TypeDescriptor elementType) => new()
    {
        CSharpName = $"{elementType.CSharpName}[]",
        FullName = $"{elementType.FullName}[]",
        TypeScriptName = $"{elementType.TypeScriptName}[]",
        IsPrimitive = false,
        IsNullable = false,
        IsArray = true,
        IsDictionary = false,
        IsEnum = false,
        ElementType = elementType
    };

    public static TypeDescriptor CreateDictionaryType(TypeDescriptor keyType, TypeDescriptor valueType) => new()
    {
        CSharpName = $"Dictionary<{keyType.CSharpName}, {valueType.CSharpName}>",
        FullName = $"System.Collections.Generic.Dictionary<{keyType.FullName}, {valueType.FullName}>",
        TypeScriptName = $"Record<{keyType.TypeScriptName}, {valueType.TypeScriptName}>",
        IsPrimitive = false,
        IsNullable = false,
        IsArray = false,
        IsDictionary = true,
        IsEnum = false,
        KeyType = keyType,
        ValueType = valueType
    };

    public static TypeDescriptor CreateComplexType(string name) => new()
    {
        CSharpName = name,
        FullName = $"Test.{name}",
        TypeScriptName = name,
        IsPrimitive = false,
        IsNullable = false,
        IsArray = false,
        IsDictionary = false,
        IsEnum = false
    };

    public static TypeDescriptor CreateEnumType(string name) => new()
    {
        CSharpName = name,
        FullName = $"Test.{name}",
        TypeScriptName = name,
        IsPrimitive = false,
        IsNullable = false,
        IsArray = false,
        IsDictionary = false,
        IsEnum = true
    };

    public static PropertyDescriptor CreateProperty(
        string csharpName,
        string jsonName,
        TypeDescriptor type,
        bool isOptional = false) => new()
    {
        CSharpName = csharpName,
        JsonName = jsonName,
        TypeScriptName = char.ToLowerInvariant(csharpName[0]) + csharpName[1..],
        Type = type,
        IsRequired = !isOptional,
        IsOptional = isOptional
    };

    public static PropertyDescriptor CreateProperty(
        string csharpName,
        TypeDescriptor type,
        bool isOptional = false) => 
        CreateProperty(csharpName, csharpName, type, isOptional);

    public static ModelDescriptor CreateModel(
        string name,
        params PropertyDescriptor[] properties) => new()
    {
        Name = name,
        FullName = $"Test.{name}",
        Namespace = "Test",
        Properties = properties,
        IsEnum = false
    };

    public static ModelDescriptor CreateEnum(
        string name,
        params (string Name, int Value)[] values) => new()
    {
        Name = name,
        FullName = $"Test.{name}",
        Namespace = "Test",
        Properties = [],
        IsEnum = true,
        EnumValues = values.Select(v => new EnumValueDescriptor 
        { 
            Name = v.Name, 
            Value = v.Value 
        }).ToList()
    };

    // Date TypeDescriptors
    public static TypeDescriptor DateTimeType => new()
    {
        CSharpName = "DateTime",
        FullName = "System.DateTime",
        TypeScriptName = "string",
        IsPrimitive = true,
        IsNullable = false,
        IsArray = false,
        IsDictionary = false,
        IsEnum = false,
        DateKind = DateTypeKind.DateTime
    };

    public static TypeDescriptor DateTimeOffsetType => new()
    {
        CSharpName = "DateTimeOffset",
        FullName = "System.DateTimeOffset",
        TypeScriptName = "string",
        IsPrimitive = true,
        IsNullable = false,
        IsArray = false,
        IsDictionary = false,
        IsEnum = false,
        DateKind = DateTypeKind.DateTimeOffset
    };

    public static TypeDescriptor DateOnlyType => new()
    {
        CSharpName = "DateOnly",
        FullName = "System.DateOnly",
        TypeScriptName = "string",
        IsPrimitive = true,
        IsNullable = false,
        IsArray = false,
        IsDictionary = false,
        IsEnum = false,
        DateKind = DateTypeKind.DateOnly
    };

    public static TypeDescriptor CreateGenericParameterType(string name = "T") => new()
    {
        CSharpName = name,
        FullName = name,
        TypeScriptName = name,
        IsPrimitive = false,
        IsNullable = false,
        IsArray = false,
        IsDictionary = false,
        IsEnum = false,
        IsGenericParameter = true
    };
}
