using RepositoryFramework.Tools.TypescriptGenerator.Utils;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for GenericTypeHelper - parsing and normalizing generic type names.
/// </summary>
public class GenericTypeHelperTest
{
    [Fact]
    public void IsGenericType_WithAngleBrackets_ReturnsTrue()
    {
        // Act
        var result = GenericTypeHelper.IsGenericType("EntityVersions<Timeline>");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsGenericType_WithBacktick_ReturnsTrue()
    {
        // Act
        var result = GenericTypeHelper.IsGenericType("EntityVersions`1[[Timeline]]");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsGenericType_WithoutGenerics_ReturnsFalse()
    {
        // Act
        var result = GenericTypeHelper.IsGenericType("Timeline");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Parse_UserFriendlySyntax_ParsesCorrectly()
    {
        // Arrange
        var typeName = "EntityVersions<Timeline>";

        // Act
        var result = GenericTypeHelper.Parse(typeName);

        // Assert
        Assert.True(result.IsGeneric);
        Assert.Equal("EntityVersions", result.BaseTypeName);
        Assert.Single(result.TypeArguments);
        Assert.Equal("Timeline", result.TypeArguments[0]);
        Assert.Equal("EntityVersions<Timeline>", result.DisplayName);
        Assert.Equal("EntityVersions`1", result.ReflectionName);
    }

    [Fact]
    public void Parse_ReflectionSyntax_ParsesCorrectly()
    {
        // Arrange
        var typeName = "EntityVersions`1[[GhostWriter.Business.Timeline]]";

        // Act
        var result = GenericTypeHelper.Parse(typeName);

        // Assert
        Assert.True(result.IsGeneric);
        Assert.Equal("EntityVersions", result.BaseTypeName);
        Assert.Single(result.TypeArguments);
        Assert.Equal("GhostWriter.Business.Timeline", result.TypeArguments[0]);
        Assert.Equal("EntityVersions<Timeline>", result.DisplayName); // Simple name in display
        Assert.Equal("EntityVersions`1", result.ReflectionName);
    }

    [Fact]
    public void Parse_ReflectionSyntaxWithAssemblyInfo_ParsesCorrectly()
    {
        // Arrange
        var typeName = "EntityVersions`1[[GhostWriter.Business.Timeline, MyAssembly]]";

        // Act
        var result = GenericTypeHelper.Parse(typeName);

        // Assert
        Assert.True(result.IsGeneric);
        Assert.Equal("EntityVersions", result.BaseTypeName);
        Assert.Single(result.TypeArguments);
        Assert.Equal("GhostWriter.Business.Timeline", result.TypeArguments[0]); // Assembly info stripped
    }

    [Fact]
    public void Parse_MultipleTypeArguments_ParsesAll()
    {
        // Arrange
        var typeName = "Dictionary<string, int>";

        // Act
        var result = GenericTypeHelper.Parse(typeName);

        // Assert
        Assert.True(result.IsGeneric);
        Assert.Equal("Dictionary", result.BaseTypeName);
        Assert.Equal(2, result.TypeArguments.Count);
        Assert.Equal("string", result.TypeArguments[0]);
        Assert.Equal("int", result.TypeArguments[1]);
        Assert.Equal("Dictionary`2", result.ReflectionName);
    }

    [Fact]
    public void Parse_NestedGenerics_ParsesCorrectly()
    {
        // Arrange
        var typeName = "List<Dictionary<string, int>>";

        // Act
        var result = GenericTypeHelper.Parse(typeName);

        // Assert
        Assert.True(result.IsGeneric);
        Assert.Equal("List", result.BaseTypeName);
        Assert.Single(result.TypeArguments);
        Assert.Equal("Dictionary<string, int>", result.TypeArguments[0]);
        Assert.Equal("List`1", result.ReflectionName);
    }

    [Fact]
    public void Parse_NonGenericType_ReturnsNonGenericInfo()
    {
        // Arrange
        var typeName = "Timeline";

        // Act
        var result = GenericTypeHelper.Parse(typeName);

        // Assert
        Assert.False(result.IsGeneric);
        Assert.Equal("Timeline", result.BaseTypeName);
        Assert.Empty(result.TypeArguments);
        Assert.Equal("Timeline", result.DisplayName);
        Assert.Equal("Timeline", result.ReflectionName);
    }

    [Fact]
    public void ToReflectionTypeName_UserFriendly_ConvertsCorrectly()
    {
        // Act
        var result = GenericTypeHelper.ToReflectionTypeName("EntityVersions<Timeline>");

        // Assert
        Assert.Equal("EntityVersions`1", result);
    }

    [Fact]
    public void ToReflectionTypeName_ReflectionSyntax_ReturnsBaseWithBacktick()
    {
        // Act
        var result = GenericTypeHelper.ToReflectionTypeName("EntityVersions`1[[Timeline]]");

        // Assert
        Assert.Equal("EntityVersions`1", result);
    }

    [Fact]
    public void ToReflectionTypeName_NonGeneric_ReturnsAsIs()
    {
        // Act
        var result = GenericTypeHelper.ToReflectionTypeName("Timeline");

        // Assert
        Assert.Equal("Timeline", result);
    }

    [Fact]
    public void ToUserFriendlyName_ReflectionSyntax_ConvertsCorrectly()
    {
        // Act
        var result = GenericTypeHelper.ToUserFriendlyName("EntityVersions`1[[GhostWriter.Business.Timeline]]");

        // Assert
        Assert.Equal("EntityVersions<Timeline>", result);
    }

    [Fact]
    public void ToUserFriendlyName_UserFriendly_ReturnsAsIs()
    {
        // Act
        var result = GenericTypeHelper.ToUserFriendlyName("EntityVersions<Timeline>");

        // Assert
        Assert.Equal("EntityVersions<Timeline>", result);
    }

    [Fact]
    public void ToUserFriendlyName_NonGeneric_ReturnsAsIs()
    {
        // Act
        var result = GenericTypeHelper.ToUserFriendlyName("Timeline");

        // Assert
        Assert.Equal("Timeline", result);
    }

    [Fact]
    public void Parse_FullyQualifiedGeneric_ParsesCorrectly()
    {
        // Arrange
        var typeName = "GhostWriter.Infrastructure.EntityVersions<GhostWriter.Business.Timeline>";

        // Act
        var result = GenericTypeHelper.Parse(typeName);

        // Assert
        Assert.True(result.IsGeneric);
        Assert.Equal("GhostWriter.Infrastructure.EntityVersions", result.BaseTypeName);
        Assert.Single(result.TypeArguments);
        Assert.Equal("GhostWriter.Business.Timeline", result.TypeArguments[0]);
    }

    [Fact]
    public void Parse_MultipleTypeArgumentsReflection_ParsesAll()
    {
        // Arrange
        var typeName = "Dictionary`2[[System.String]][[System.Int32]]";

        // Act
        var result = GenericTypeHelper.Parse(typeName);

        // Assert
        Assert.True(result.IsGeneric);
        Assert.Equal("Dictionary", result.BaseTypeName);
        Assert.Equal(2, result.TypeArguments.Count);
        Assert.Equal("System.String", result.TypeArguments[0]);
        Assert.Equal("System.Int32", result.TypeArguments[1]);
    }

    [Fact]
    public void Parse_NestedGenericWithAssemblyQualifiedName_ParsesCorrectly()
    {
        // Arrange - This is the problematic case that caused stack overflow with regex
        var typeName = "EntityVersions`1[[GhostWriter.Core.EntityVersion`1[[GhostWriter.Core.Book, GhostWriter.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], GhostWriter.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]";

        // Act
        var result = GenericTypeHelper.Parse(typeName);

        // Assert
        Assert.True(result.IsGeneric);
        Assert.Equal("EntityVersions", result.BaseTypeName);
        Assert.Single(result.TypeArguments);

        // Should extract only the type name, not the assembly info
        var typeArg = result.TypeArguments[0];
        Assert.StartsWith("GhostWriter.Core.EntityVersion`1", typeArg);
        Assert.DoesNotContain("Version=", typeArg);
        Assert.DoesNotContain("Culture=", typeArg);
        Assert.DoesNotContain("PublicKeyToken=", typeArg);
    }

    [Fact]
    public void Parse_DeeplyNestedGenerics_HandlesBracketDepthCorrectly()
    {
        // Arrange - Multiple levels of nesting
        var typeName = "Outer`1[[Middle`1[[Inner`1[[System.String, mscorlib]], MyAssembly]], MyAssembly]]";

        // Act
        var result = GenericTypeHelper.Parse(typeName);

        // Assert
        Assert.True(result.IsGeneric);
        Assert.Equal("Outer", result.BaseTypeName);
        Assert.Single(result.TypeArguments);
        Assert.StartsWith("Middle`1", result.TypeArguments[0]);
    }

    [Fact]
    public void Parse_GenericWithCommasInAssemblyName_ExtractsTypeNameOnly()
    {
        // Arrange
        var typeName = "List`1[[GhostWriter.Core.Book, GhostWriter.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]";

        // Act
        var result = GenericTypeHelper.Parse(typeName);

        // Assert
        Assert.True(result.IsGeneric);
        Assert.Equal("List", result.BaseTypeName);
        Assert.Single(result.TypeArguments);
        Assert.Equal("GhostWriter.Core.Book", result.TypeArguments[0]);
    }

    [Fact]
    public void Parse_MultipleNestedGenericsWithAssemblyInfo_ParsesAll()
    {
        // Arrange
        var typeName = "Dictionary`2[[System.String, mscorlib, Version=4.0.0.0]][[List`1[[System.Int32, mscorlib]], mscorlib]]";

        // Act
        var result = GenericTypeHelper.Parse(typeName);

        // Assert
        Assert.True(result.IsGeneric);
        Assert.Equal("Dictionary", result.BaseTypeName);
        Assert.Equal(2, result.TypeArguments.Count);
        Assert.Equal("System.String", result.TypeArguments[0]);
        Assert.StartsWith("List`1", result.TypeArguments[1]);
    }
}

