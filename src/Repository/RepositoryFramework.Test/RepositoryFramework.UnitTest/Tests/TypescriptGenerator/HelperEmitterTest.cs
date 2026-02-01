using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using Xunit;
using static RepositoryFramework.UnitTest.TypescriptGenerator.TestDescriptorFactory;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for HelperEmitter - generating TypeScript helper classes.
/// </summary>
public class HelperEmitterTest
{
    private static EmitterContext CreateContext() => new();

    [Fact]
    public void Emit_ModelWithArrayProperty_GeneratesHelper()
    {
        // Arrange
        var model = CreateModel("Team",
            CreateProperty("Players", "players", CreateArrayType(StringType)));
        var context = CreateContext();

        // Act
        var result = HelperEmitter.Emit(model, context);

        // Assert
        Assert.Contains("export class TeamHelper", result);
        Assert.Contains("getPlayers", result);
    }

    [Fact]
    public void Emit_ModelWithDictionaryProperty_GeneratesKeyGetter()
    {
        // Arrange
        var model = CreateModel("Settings",
            CreateProperty("Options", "options", CreateDictionaryType(StringType, NumberType)));
        var context = CreateContext();

        // Act
        var result = HelperEmitter.Emit(model, context);

        // Assert
        Assert.Contains("export class SettingsHelper", result);
        Assert.Contains("getOptionsKeys", result);
        Assert.Contains("getOptionsValue", result);
    }

    [Fact]
    public void Emit_ModelWithOnlyPrimitives_ReturnsEmpty()
    {
        // Arrange
        var model = CreateModel("Simple",
            CreateProperty("Name", StringType),
            CreateProperty("Age", NumberType));
        var context = CreateContext();

        // Act
        var result = HelperEmitter.Emit(model, context);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Emit_EnumModel_ReturnsEmpty()
    {
        // Arrange
        var model = CreateEnum("Status", ("Active", 0));
        var context = CreateContext();

        // Act
        var result = HelperEmitter.Emit(model, context);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Emit_ArrayGetter_ReturnsCorrectReturnStatement()
    {
        // Arrange
        var model = CreateModel("Container",
            CreateProperty("Items", "items", CreateArrayType(NumberType)));
        var context = CreateContext();

        // Act
        var result = HelperEmitter.Emit(model, context);

        // Assert
        Assert.Contains("return container.items ?? [];", result);
    }

    [Fact]
    public void Emit_DictionaryValueGetter_ReturnsOptionalType()
    {
        // Arrange
        var model = CreateModel("Cache",
            CreateProperty("Data", "data", CreateDictionaryType(StringType, CreateComplexType("User"))));
        var context = CreateContext();

        // Act
        var result = HelperEmitter.Emit(model, context);

        // Assert
        Assert.Contains(": User | undefined", result);
        Assert.Contains("?.[key]", result);
    }

    [Fact]
    public void Emit_MultipleCollectionProperties_GeneratesMultipleHelpers()
    {
        // Arrange
        var model = CreateModel("Complex",
            CreateProperty("Names", "names", CreateArrayType(StringType)),
            CreateProperty("Scores", "scores", CreateArrayType(NumberType)),
            CreateProperty("Settings", "settings", CreateDictionaryType(StringType, StringType)));
        var context = CreateContext();

        // Act
        var result = HelperEmitter.Emit(model, context);

        // Assert
        Assert.Contains("getNames", result);
        Assert.Contains("getScores", result);
        Assert.Contains("getSettingsKeys", result);
        Assert.Contains("getSettingsValue", result);
    }

    [Fact]
    public void Emit_HelperClass_UsesStaticMethods()
    {
        // Arrange
        var model = CreateModel("Data",
            CreateProperty("Items", "items", CreateArrayType(StringType)));
        var context = CreateContext();

        // Act
        var result = HelperEmitter.Emit(model, context);

        // Assert
        Assert.Contains("static getItems", result);
    }

    [Fact]
    public void Emit_ParameterName_UsesCamelCase()
    {
        // Arrange
        var model = CreateModel("TeamData",
            CreateProperty("Players", "players", CreateArrayType(StringType)));
        var context = CreateContext();

        // Act
        var result = HelperEmitter.Emit(model, context);

        // Assert
        Assert.Contains("(teamData: TeamData)", result);
    }

    [Fact]
    public void EmitAll_MultipleModels_OnlyGeneratesForModelsWithCollections()
    {
        // Arrange
        var context = CreateContext();
        var models = new[]
        {
            CreateModel("WithArray", CreateProperty("Items", "items", CreateArrayType(StringType))),
            CreateModel("WithoutArray", CreateProperty("Name", StringType))
        };

        // Act
        var result = HelperEmitter.EmitAll(models, context);

        // Assert
        Assert.Contains("WithArrayHelper", result);
        Assert.DoesNotContain("WithoutArrayHelper", result);
    }
}
