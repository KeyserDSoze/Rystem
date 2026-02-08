using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using Xunit;
using static RepositoryFramework.UnitTest.TypescriptGenerator.TestDescriptorFactory;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for MapperEmitter - generating TypeScript mapping functions.
/// </summary>
public class MapperEmitterTest
{
    private static EmitterContext CreateContext()
    {
        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("User");
        context.TypesRequiringRaw.Add("Address");
        context.TypesRequiringRaw.Add("Order");
        return context;
    }

    [Fact]
    public void Emit_SimpleModel_GeneratesBothMappers()
    {
        // Arrange
        var model = CreateModel("User",
            CreateProperty("Name", "n", StringType),
            CreateProperty("Age", "a", NumberType));
        var context = CreateContext();

        // Act
        var result = MapperEmitter.Emit(model, context);

        // Assert
        Assert.Contains("export const mapRawUserToUser", result);
        Assert.Contains("(raw: UserRaw): User", result);
        Assert.Contains("export const mapUserToRawUser", result);
        Assert.Contains("(clean: User): UserRaw", result);
    }

    [Fact]
    public void EmitRawToClean_MapsJsonNamesToCleanNames()
    {
        // Arrange
        var model = CreateModel("User",
            CreateProperty("FirstName", "fn", StringType));
        var context = CreateContext();

        // Act
        var result = MapperEmitter.EmitRawToClean(model, context);

        // Assert
        Assert.Contains("firstName:", result);
        Assert.Contains("raw.fn", result);
    }

    [Fact]
    public void EmitCleanToRaw_MapsCleanNamesToJsonNames()
    {
        // Arrange
        var model = CreateModel("User",
            CreateProperty("FirstName", "fn", StringType));
        var context = CreateContext();

        // Act
        var result = MapperEmitter.EmitCleanToRaw(model, context);

        // Assert
        Assert.Contains("fn:", result);
        Assert.Contains("clean.firstName", result);
    }

    [Fact]
    public void Emit_WithArrayOfPrimitives_GeneratesArrayMapping()
    {
        // Arrange
        var model = CreateModel("User",
            CreateProperty("Tags", "t", CreateArrayType(StringType)));
        var context = CreateContext();

        // Act
        var result = MapperEmitter.Emit(model, context);

        // Assert
        Assert.Contains("raw.t ?? []", result);
        Assert.Contains("clean.tags ?? []", result);
    }

    [Fact]
    public void Emit_WithArrayOfComplexTypes_GeneratesNestedMapping()
    {
        // Arrange
        var context = CreateContext();
        context.TypesRequiringRaw.Add("OrderItem");

        var model = CreateModel("Order",
            CreateProperty("Items", "items", CreateArrayType(CreateComplexType("OrderItem"))));

        // Act
        var result = MapperEmitter.Emit(model, context);

        // Assert
        Assert.Contains("mapRawOrderItemToOrderItem", result);
        Assert.Contains(".map(", result);
    }

    [Fact]
    public void Emit_EnumModel_ReturnsEmpty()
    {
        // Arrange
        var model = CreateEnum("Status", ("Active", 0));
        var context = CreateContext();

        // Act
        var result = MapperEmitter.Emit(model, context);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Emit_ModelNotRequiringRaw_ReturnsEmpty()
    {
        // Arrange - model without custom JSON names doesn't require raw
        var model = CreateModel("SimpleModel",
            CreateProperty("Id", NumberType)); // JsonName == CSharpName
        var context = new EmitterContext(); // Empty context - SimpleModel not in TypesRequiringRaw

        // Act
        var result = MapperEmitter.Emit(model, context);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Emit_MapperFunctionNames_FollowConvention()
    {
        // Arrange
        var model = CreateModel("CalendarDay",
            CreateProperty("Id", "i", NumberType));
        var context = CreateContext();
        context.TypesRequiringRaw.Add("CalendarDay");

        // Act
        var result = MapperEmitter.Emit(model, context);

        // Assert
        Assert.Contains("mapRawCalendarDayToCalendarDay", result);
        Assert.Contains("mapCalendarDayToRawCalendarDay", result);
    }

    [Fact]
    public void Emit_WithBooleanProperty_MapsCorrectly()
    {
        // Arrange
        var model = CreateModel("User",
            CreateProperty("IsActive", "a", BooleanType));
        var context = CreateContext();

        // Act
        var result = MapperEmitter.Emit(model, context);

        // Assert
        Assert.Contains("isActive:", result);
        Assert.Contains("raw.a", result);
        Assert.Contains("a:", result);
        Assert.Contains("clean.isActive", result);
    }

    [Fact]
    public void Emit_WithEnumProperty_DirectMapping()
    {
        // Arrange
        var context = CreateContext();
        context.EnumTypes.Add("Status");

        var model = CreateModel("User",
            CreateProperty("Status", "s", CreateEnumType("Status")));

        // Act
        var result = MapperEmitter.Emit(model, context);

        // Assert
        Assert.Contains("raw.s", result);
        Assert.Contains("clean.status", result);
        Assert.DoesNotContain("mapRawStatusToStatus", result);
    }

    [Fact]
    public void Emit_OptionalComplexType_UsesUndefinedNotNull()
    {
        // Arrange — optional complex property must use `undefined` (not `null`)
        // because TypeScript `?:` means `T | undefined`, and `null` is not assignable to `undefined`.
        var context = CreateContext();
        context.TypesRequiringRaw.Add("HistoricalPeriod");

        var model = CreateModel("Book",
            CreateProperty("HistoricalPeriod", "p", CreateComplexType("HistoricalPeriod"), isOptional: true));

        // Act
        var result = MapperEmitter.Emit(model, context);

        // Assert — Raw->Clean: must use undefined
        Assert.Contains("raw.p ? mapRawHistoricalPeriodToHistoricalPeriod(raw.p) : undefined", result);
        Assert.DoesNotContain(": null", result);

        // Assert — Clean->Raw: must also use undefined
        Assert.Contains("clean.historicalPeriod ? mapHistoricalPeriodToRawHistoricalPeriod(clean.historicalPeriod) : undefined", result);
    }
}
