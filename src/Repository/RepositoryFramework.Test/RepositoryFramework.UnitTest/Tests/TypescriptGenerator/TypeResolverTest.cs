using System;
using System.Collections.Generic;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.Tools.TypescriptGenerator.Rules;
using RepositoryFramework.UnitTest.TypescriptGenerator.Models;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for TypeResolver - mapping C# types to TypeScript.
/// </summary>
public class TypeResolverTest
{
    private readonly TypeResolver _resolver = new();

    public static IEnumerable<object[]> PrimitiveTypeTestData()
    {
        yield return [typeof(string), "string"];
        yield return [typeof(char), "string"];
        yield return [typeof(int), "number"];
        yield return [typeof(long), "number"];
        yield return [typeof(short), "number"];
        yield return [typeof(byte), "number"];
        yield return [typeof(float), "number"];
        yield return [typeof(double), "number"];
        yield return [typeof(decimal), "number"];
        yield return [typeof(bool), "boolean"];
        yield return [typeof(DateTime), "string"];
        yield return [typeof(DateTimeOffset), "string"];
        yield return [typeof(TimeSpan), "string"];
        yield return [typeof(Guid), "string"];
    }

    [Theory]
    [MemberData(nameof(PrimitiveTypeTestData))]
    public void Resolve_PrimitiveTypes_MapsCorrectly(Type csharpType, string expectedTsType)
    {
        // Act
        var result = _resolver.Resolve(csharpType);

        // Assert
        Assert.Equal(expectedTsType, result.TypeScriptName);
        Assert.True(result.IsPrimitive);
        Assert.False(result.IsArray);
        Assert.False(result.IsDictionary);
        Assert.False(result.IsEnum);
    }

    [Fact]
    public void Resolve_NullableValueType_IsNullable()
    {
        // Act
        var result = _resolver.Resolve(typeof(int?));

        // Assert
        Assert.Equal("number", result.TypeScriptName);
        Assert.True(result.IsNullable);
        Assert.True(result.IsPrimitive);
    }

    [Fact]
    public void Resolve_Array_ReturnsArrayDescriptor()
    {
        // Act
        var result = _resolver.Resolve(typeof(int[]));

        // Assert
        Assert.True(result.IsArray);
        Assert.NotNull(result.ElementType);
        Assert.Equal("number", result.ElementType!.TypeScriptName);
        Assert.Contains("[]", result.TypeScriptName);
    }

    [Fact]
    public void Resolve_List_ReturnsArrayDescriptor()
    {
        // Act
        var result = _resolver.Resolve(typeof(List<string>));

        // Assert
        Assert.True(result.IsArray);
        Assert.NotNull(result.ElementType);
        Assert.Equal("string", result.ElementType!.TypeScriptName);
    }

    [Fact]
    public void Resolve_Dictionary_ReturnsDictionaryDescriptor()
    {
        // Act
        var result = _resolver.Resolve(typeof(Dictionary<string, int>));

        // Assert
        Assert.True(result.IsDictionary);
        Assert.NotNull(result.KeyType);
        Assert.NotNull(result.ValueType);
        Assert.Equal("string", result.KeyType!.TypeScriptName);
        Assert.Equal("number", result.ValueType!.TypeScriptName);
        Assert.Contains("Record<", result.TypeScriptName);
    }

    [Fact]
    public void Resolve_DictionaryWithComplexValue_ReturnsCorrectDescriptor()
    {
        // Act
        var result = _resolver.Resolve(typeof(Dictionary<string, Models.CalendarDay[]>));

        // Assert
        Assert.True(result.IsDictionary);
        Assert.Equal("string", result.KeyType!.TypeScriptName);
        Assert.True(result.ValueType!.IsArray);
        Assert.Equal("CalendarDay", result.ValueType!.ElementType!.TypeScriptName);
    }

    [Fact]
    public void Resolve_Enum_ReturnsEnumDescriptor()
    {
        // Act
        var result = _resolver.Resolve(typeof(Models.PlayerRole));

        // Assert
        Assert.True(result.IsEnum);
        Assert.False(result.IsPrimitive);
        Assert.Equal("PlayerRole", result.TypeScriptName);
    }

    [Fact]
    public void Resolve_ComplexType_ReturnsComplexDescriptor()
    {
        // Act
        var result = _resolver.Resolve(typeof(Models.Calendar));

        // Assert
        Assert.False(result.IsPrimitive);
        Assert.False(result.IsArray);
        Assert.False(result.IsDictionary);
        Assert.False(result.IsEnum);
        Assert.Equal("Calendar", result.TypeScriptName);
    }

    [Fact]
    public void Resolve_IEnumerable_ReturnsArrayDescriptor()
    {
        // Act
        var result = _resolver.Resolve(typeof(IEnumerable<Models.Player>));

        // Assert
        Assert.True(result.IsArray);
        Assert.NotNull(result.ElementType);
        Assert.Equal("Player", result.ElementType!.TypeScriptName);
    }

    [Fact]
    public void Resolve_CachesResults()
    {
        // Act
        var result1 = _resolver.Resolve(typeof(Models.Calendar));
        var result2 = _resolver.Resolve(typeof(Models.Calendar));

        // Assert - should be same instance due to caching
        Assert.Same(result1, result2);
    }

    [Fact]
    public void Resolve_TypeWithJsonConverterSingleStringProperty_TreatedAsString()
    {
        // Arrange - LocalizedFormatString has [JsonConverter] and single 'Value: string' property
        // Act
        var result = _resolver.Resolve(typeof(LocalizedFormatString));

        // Assert - should be resolved as "string", not as a complex type
        Assert.Equal("string", result.TypeScriptName);
        Assert.True(result.IsPrimitive);
        Assert.False(result.IsArray);
        Assert.False(result.IsDictionary);
    }

    [Fact]
    public void Resolve_TypeWithJsonConverterSingleIntProperty_TreatedAsNumber()
    {
        // Arrange - WrappedInt has [JsonConverter] and single 'Value: int' property
        // Act
        var result = _resolver.Resolve(typeof(WrappedInt));

        // Assert
        Assert.Equal("number", result.TypeScriptName);
        Assert.True(result.IsPrimitive);
    }

    [Fact]
    public void Resolve_TypeWithJsonConverterMultipleProperties_StaysComplex()
    {
        // Arrange - MultiPropertyWithConverter has [JsonConverter] that calls WriteStartObject
        // IL analysis detects complex output → stays as complex type
        // Act
        var result = _resolver.Resolve(typeof(MultiPropertyWithConverter));

        // Assert
        Assert.False(result.IsPrimitive);
        Assert.Equal("MultiPropertyWithConverter", result.TypeScriptName);
    }

    [Fact]
    public void Resolve_ChapterLocalization_DescriptionIsString()
    {
        // Arrange - ChapterLocalization.Description is LocalizedFormatString
        // which has a [JsonConverter] → should resolve to "string"
        // Act
        var result = _resolver.Resolve(typeof(ChapterLocalization));

        // Assert - the type itself is complex
        Assert.False(result.IsPrimitive);

        // But when we resolve the property type directly
        var descriptionType = _resolver.Resolve(typeof(LocalizedFormatString));
        Assert.Equal("string", descriptionType.TypeScriptName);
        Assert.True(descriptionType.IsPrimitive);
    }

    [Fact]
    public void Resolve_OpaqueToken_ZeroPropertiesButConverterWritesString_TreatedAsString()
    {
        // Arrange - OpaqueToken has ZERO public properties,
        // but its converter calls WriteStringValue → IL analysis detects "string"
        // Old heuristic would FAIL here (requires exactly 1 property)
        // Act
        var result = _resolver.Resolve(typeof(OpaqueToken));

        // Assert
        Assert.Equal("string", result.TypeScriptName);
        Assert.True(result.IsPrimitive);
    }

    [Fact]
    public void Resolve_ScoreValue_MultiplePropertiesButConverterWritesNumber_TreatedAsNumber()
    {
        // Arrange - ScoreValue has 3 properties (Points, Category, EarnedAt),
        // but its converter calls WriteNumberValue → IL analysis detects "number"
        // Old heuristic would FAIL here (more than 1 property)
        // Act
        var result = _resolver.Resolve(typeof(ScoreValue));

        // Assert
        Assert.Equal("number", result.TypeScriptName);
        Assert.True(result.IsPrimitive);
    }

    [Fact]
    public void Resolve_Flag_ConverterWritesBoolean_TreatedAsBoolean()
    {
        // Arrange - Flag has 2 properties but converter calls WriteBooleanValue
        // Act
        var result = _resolver.Resolve(typeof(Flag));

        // Assert
        Assert.Equal("boolean", result.TypeScriptName);
        Assert.True(result.IsPrimitive);
    }
}

/// <summary>
/// Tests for PrimitiveTypeRules.
/// </summary>
public class PrimitiveTypeRulesTest
{
    [Theory]
    [InlineData("string", true)]
    [InlineData("int", true)]
    [InlineData("Int32", true)]
    [InlineData("long", true)]
    [InlineData("Int64", true)]
    [InlineData("Guid", true)]
    [InlineData("DateTime", true)]
    [InlineData("bool", true)]
    [InlineData("Boolean", true)]
    [InlineData("Calendar", false)]
    [InlineData("LeagueKey", false)]
    [InlineData("CustomType", false)]
    public void IsPrimitiveName_ReturnsCorrectResult(string typeName, bool expected)
    {
        // Act
        var result = PrimitiveTypeRules.IsPrimitiveName(typeName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("string", "string")]
    [InlineData("int", "number")]
    [InlineData("Int32", "number")]
    [InlineData("Guid", "string")]
    [InlineData("DateTime", "string")]
    [InlineData("bool", "boolean")]
    [InlineData("CustomType", "CustomType")] // Returns as-is for non-primitives
    public void GetTypeScriptTypeFromName_ReturnsCorrectType(string typeName, string expected)
    {
        // Act
        var result = PrimitiveTypeRules.GetTypeScriptTypeFromName(typeName);

        // Assert
        Assert.Equal(expected, result);
    }
}
