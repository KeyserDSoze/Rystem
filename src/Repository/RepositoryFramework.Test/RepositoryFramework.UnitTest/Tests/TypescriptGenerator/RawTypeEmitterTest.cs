using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using Xunit;
using static RepositoryFramework.UnitTest.TypescriptGenerator.TestDescriptorFactory;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for RawTypeEmitter - generating TypeScript Raw interfaces.
/// </summary>
public class RawTypeEmitterTest
{
    private static EmitterContext CreateContext()
    {
        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Calendar");
        context.TypesRequiringRaw.Add("CalendarDay");
        return context;
    }

    [Fact]
    public void Emit_SimpleModel_GeneratesRawInterface()
    {
        // Arrange
        var model = CreateModel("User",
            CreateProperty("Name", "n", StringType),
            CreateProperty("Age", "a", NumberType));
        var context = CreateContext();

        // Act
        var result = RawTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("export interface UserRaw {", result);
        Assert.Contains("n: string;", result);
        Assert.Contains("a: number;", result);
    }

    [Fact]
    public void Emit_WithOptionalProperties_AddsQuestionMark()
    {
        // Arrange
        var model = CreateModel("Profile",
            CreateProperty("Email", "email", StringType, isOptional: true),
            CreateProperty("Id", "id", NumberType, isOptional: false));
        var context = CreateContext();

        // Act
        var result = RawTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("email?: string;", result);
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
        var result = RawTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("p: string[];", result);
    }

    [Fact]
    public void Emit_WithComplexArrayProperty_UsesRawTypeName()
    {
        // Arrange
        var context = CreateContext();
        context.TypesRequiringRaw.Add("Player");

        var model = CreateModel("Team",
            CreateProperty("Players", "players", CreateArrayType(CreateComplexType("Player"))));

        // Act
        var result = RawTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("players: PlayerRaw[];", result);
    }

    [Fact]
    public void Emit_WithDictionaryProperty_GeneratesRecordType()
    {
        // Arrange
        var model = CreateModel("Settings",
            CreateProperty("Options", "opts", CreateDictionaryType(StringType, StringType)));
        var context = CreateContext();

        // Act
        var result = RawTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("opts: Record<string, string>;", result);
    }

    [Fact]
    public void Emit_WithEnumProperty_UsesEnumNameDirectly()
    {
        // Arrange
        var context = CreateContext();
        context.EnumTypes.Add("Status");

        var model = CreateModel("Task",
            CreateProperty("Status", "s", CreateEnumType("Status")));

        // Act
        var result = RawTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("s: Status;", result);
        Assert.DoesNotContain("StatusRaw", result);
    }

    [Fact]
    public void Emit_EnumDescriptor_ThrowsArgumentException()
    {
        // Arrange
        var enumDescriptor = CreateEnum("MyEnum", ("Value", 0));
        var context = CreateContext();

        // Act & Assert
        Assert.Throws<System.ArgumentException>(() => RawTypeEmitter.Emit(enumDescriptor, context));
    }

    [Fact]
    public void Emit_PrimitiveTypes_UsesCorrectTypeScriptTypes()
    {
        // Arrange
        var model = CreateModel("AllTypes",
            CreateProperty("Text", "text", StringType),
            CreateProperty("Count", "count", NumberType),
            CreateProperty("IsEnabled", "enabled", BooleanType));
        var context = CreateContext();

        // Act
        var result = RawTypeEmitter.Emit(model, context);

        // Assert
        Assert.Contains("text: string;", result);
        Assert.Contains("count: number;", result);
        Assert.Contains("enabled: boolean;", result);
    }

    [Fact]
    public void EmitAll_MultipleModels_GeneratesOnlyModelsRequiringRaw()
    {
        // Arrange
        var context = new EmitterContext();
        
        var modelWithCustomJson = CreateModel("ModelA",
            CreateProperty("Id", "id_custom", NumberType)); // Has custom JSON name
        context.TypesRequiringRaw.Add("ModelA");

        var modelWithoutCustomJson = CreateModel("ModelB",
            CreateProperty("Id", NumberType)); // No custom JSON name

        var enumModel = CreateEnum("MyEnum", ("Value", 0));

        var models = new[] { modelWithCustomJson, modelWithoutCustomJson, enumModel };

        // Act
        var result = RawTypeEmitter.EmitAll(models, context);

        // Assert
        Assert.Contains("export interface ModelARaw", result);
        Assert.DoesNotContain("ModelBRaw", result);
        Assert.DoesNotContain("MyEnumRaw", result);
    }
}
