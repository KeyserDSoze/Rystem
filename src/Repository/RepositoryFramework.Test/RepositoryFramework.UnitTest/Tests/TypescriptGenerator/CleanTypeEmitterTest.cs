using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using Xunit;
using static RepositoryFramework.UnitTest.TypescriptGenerator.TestDescriptorFactory;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for CleanTypeEmitter - generating TypeScript Clean interfaces.
/// </summary>
public class CleanTypeEmitterTest
{
    private static EmitterContext CreateContext() => new();

    [Fact]
    public void Emit_SimpleModel_GeneratesCleanInterface()
    {
        // Arrange
        var model = CreateModel("User",
            CreateProperty("Name", "n", StringType),
            CreateProperty("Age", "a", NumberType));
        var context = CreateContext();

        // Act
        var result = CleanTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("export interface User {", result);
        Assert.Contains("name: string;", result);
        Assert.Contains("age: number;", result);
        Assert.DoesNotContain("n:", result);
        Assert.DoesNotContain("a:", result);
    }

    [Fact]
    public void Emit_DoesNotAddRawSuffix()
    {
        // Arrange
        var model = CreateModel("Profile",
            CreateProperty("Bio", StringType));
        var context = CreateContext();

        // Act
        var result = CleanTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("export interface Profile", result);
        Assert.DoesNotContain("ProfileRaw", result);
    }

    [Fact]
    public void Emit_WithOptionalProperties_AddsQuestionMark()
    {
        // Arrange
        var model = CreateModel("Settings",
            CreateProperty("Theme", "theme", StringType, isOptional: true),
            CreateProperty("Id", "id", NumberType, isOptional: false));
        var context = CreateContext();

        // Act
        var result = CleanTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("theme?: string;", result);
        Assert.Contains("id: number;", result);
    }

    [Fact]
    public void Emit_WithArrayProperty_GeneratesArrayType()
    {
        // Arrange
        var model = CreateModel("Team",
            CreateProperty("Players", "p", CreateArrayType(StringType)));
        var context = CreateContext();

        // Act
        var result = CleanTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("players: string[];", result);
    }

    [Fact]
    public void Emit_WithComplexArrayProperty_UsesCleanTypeName()
    {
        // Arrange
        var context = CreateContext();
        context.TypesRequiringRaw.Add("Player");

        var model = CreateModel("Team",
            CreateProperty("Players", "players", CreateArrayType(CreateComplexType("Player"))));

        // Act
        var result = CleanTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("players: Player[];", result);
        Assert.DoesNotContain("PlayerRaw", result);
    }

    [Fact]
    public void Emit_WithDictionaryProperty_GeneratesRecordType()
    {
        // Arrange
        var model = CreateModel("Config",
            CreateProperty("Options", "opts", CreateDictionaryType(StringType, NumberType)));
        var context = CreateContext();

        // Act
        var result = CleanTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("options: Record<string, number>;", result);
    }

    [Fact]
    public void Emit_WithEnumProperty_UsesEnumName()
    {
        // Arrange
        var context = CreateContext();
        context.EnumTypes.Add("Priority");

        var model = CreateModel("Task",
            CreateProperty("Priority", "priority", CreateEnumType("Priority")));

        // Act
        var result = CleanTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("priority: Priority;", result);
    }

    [Fact]
    public void Emit_EnumDescriptor_ThrowsArgumentException()
    {
        // Arrange
        var enumDescriptor = CreateEnum("MyEnum", ("Value", 0));
        var context = CreateContext();

        // Act & Assert
        Assert.Throws<System.ArgumentException>(() => CleanTypeEmitter.Emit(enumDescriptor, context));
    }

    [Fact]
    public void EmitAll_MultipleModels_GeneratesAllNonEnumModels()
    {
        // Arrange
        var context = CreateContext();
        var models = new[]
        {
            CreateModel("ModelA", CreateProperty("Id", NumberType)),
            CreateModel("ModelB", CreateProperty("Name", StringType)),
            CreateEnum("MyEnum", ("Value", 0))
        };

        // Act
        var result = CleanTypeEmitter.EmitAll(models, context);

        // Assert
        Assert.Contains("export interface ModelA", result);
        Assert.Contains("export interface ModelB", result);
        Assert.DoesNotContain("MyEnum", result);
    }

    [Fact]
    public void Emit_PreservesTypeScriptPropertyName()
    {
        // Arrange
        var model = CreateModel("Entity",
            CreateProperty("FirstName", "fn", StringType));
        var context = CreateContext();

        // Act
        var result = CleanTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("firstName: string;", result);
        Assert.DoesNotContain("fn:", result);
        Assert.DoesNotContain("FirstName:", result);
    }
}
