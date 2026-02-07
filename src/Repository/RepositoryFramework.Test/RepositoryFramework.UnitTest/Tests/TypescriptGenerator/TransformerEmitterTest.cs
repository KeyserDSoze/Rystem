using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.Transformers;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for TransformerEmitter - generates ITransformer implementations.
/// </summary>
public class TransformerEmitterTest
{
    /// <summary>
    /// Creates a model with a custom JsonPropertyName so RequiresRawType is true.
    /// </summary>
    private static ModelDescriptor CreateModelWithRawType(string name) => new()
    {
        Name = name,
        FullName = $"Test.{name}",
        Namespace = "Test",
        Properties =
        [
            new PropertyDescriptor
            {
                CSharpName = "UserName",
                JsonName = "user_name",
                TypeScriptName = "userName",
                Type = TestDescriptorFactory.StringType,
                IsRequired = true,
                IsOptional = false
            }
        ],
        IsEnum = false
    };

    // ==============================
    // Tests for models WITH RequiresRawType (full transformer)
    // ==============================

    [Fact]
    public void Emit_WithRawType_GeneratesTransformerConst()
    {
        var model = CreateModelWithRawType("Calendar");
        var result = TransformerEmitter.Emit(model);
        Assert.Contains("export const CalendarTransformer: ITransformer<Calendar>", result);
    }

    [Fact]
    public void Emit_WithRawType_ImportsTypesWithImportType()
    {
        var model = CreateModelWithRawType("Calendar");
        var result = TransformerEmitter.Emit(model);
        Assert.Contains("import type { Calendar, CalendarRaw } from '../types/calendar';", result);
    }

    [Fact]
    public void Emit_WithRawType_ImportsMappersNormally()
    {
        var model = CreateModelWithRawType("Calendar");
        var result = TransformerEmitter.Emit(model);
        Assert.Contains("import { mapRawCalendarToCalendar, mapCalendarToRawCalendar } from '../types/calendar';", result);
    }

    [Fact]
    public void Emit_WithRawType_ImplementsFromPlain()
    {
        var model = CreateModelWithRawType("Calendar");
        var result = TransformerEmitter.Emit(model);
        Assert.Contains("fromPlain: (plain: CalendarRaw): Calendar => mapRawCalendarToCalendar(plain)", result);
    }

    [Fact]
    public void Emit_WithRawType_ImplementsToPlain()
    {
        var model = CreateModelWithRawType("Calendar");
        var result = TransformerEmitter.Emit(model);
        Assert.Contains("toPlain: (instance: Calendar): CalendarRaw => mapCalendarToRawCalendar(instance)", result);
    }

    [Fact]
    public void Emit_WithRawType_IncludesJsDoc()
    {
        var model = CreateModelWithRawType("Calendar");
        var result = TransformerEmitter.Emit(model);
        Assert.Contains("* Transformer for Calendar type.", result);
        Assert.Contains("* Converts between Raw (JSON) and Clean (TypeScript) representations.", result);
    }

    // ==============================
    // Tests for models WITHOUT RequiresRawType (simple pass-through)
    // ==============================

    [Fact]
    public void Emit_GeneratesTransformerConst()
    {
        // Arrange
        var model = TestDescriptorFactory.CreateModel("Calendar");

        // Act
        var result = TransformerEmitter.Emit(model);

        // Assert
        Assert.Contains("export const CalendarTransformer: ITransformer<Calendar>", result);
    }

    [Fact]
    public void Emit_ImportsITransformer()
    {
        // Arrange
        var model = TestDescriptorFactory.CreateModel("Calendar");

        // Act
        var result = TransformerEmitter.Emit(model);

        // Assert
        Assert.Contains("import type { ITransformer } from 'rystem.repository.client';", result);
    }

    [Fact]
    public void Emit_WithoutRawType_ImportsOnlyCleanType()
    {
        var model = TestDescriptorFactory.CreateModel("Calendar");
        var result = TransformerEmitter.Emit(model);

        Assert.Contains("import type { Calendar } from '../types/calendar';", result);
        Assert.DoesNotContain("CalendarRaw", result);
        Assert.DoesNotContain("mapRaw", result);
    }

    [Fact]
    public void Emit_WithoutRawType_UsesPassThrough()
    {
        var model = TestDescriptorFactory.CreateModel("Calendar");
        var result = TransformerEmitter.Emit(model);

        Assert.Contains("fromPlain: (plain: any): Calendar => plain as Calendar", result);
        Assert.Contains("toPlain: (instance: Calendar): any => instance", result);
    }

    [Fact]
    public void Emit_WithoutRawType_IncludesSimpleJsDoc()
    {
        var model = TestDescriptorFactory.CreateModel("Calendar");
        var result = TransformerEmitter.Emit(model);

        Assert.Contains("* Simple transformer for Calendar.", result);
        Assert.Contains("* The backend API returns data in the correct format, no mapping needed.", result);
    }

    // ==============================
    // Common tests
    // ==============================

    [Fact]
    public void Emit_EnumModel_ReturnsEmpty()
    {
        // Arrange
        var enumModel = TestDescriptorFactory.CreateEnum("Status", ("Active", 0), ("Inactive", 1));

        // Act
        var result = TransformerEmitter.Emit(enumModel);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Emit_KeyModel_GeneratesTransformer()
    {
        // Arrange
        var keyModel = TestDescriptorFactory.CreateModel("LeagueKey");

        // Act
        var result = TransformerEmitter.Emit(keyModel, isKey: true);

        // Assert
        Assert.Contains("export const LeagueKeyTransformer: ITransformer<LeagueKey>", result);
        Assert.Contains("fromPlain:", result);
        Assert.Contains("toPlain:", result);
    }

    [Fact]
    public void GetFileName_ReturnsCorrectName()
    {
        // Arrange
        var model = TestDescriptorFactory.CreateModel("Calendar");

        // Act
        var fileName = TransformerEmitter.GetFileName(model);

        // Assert
        Assert.Equal("CalendarTransformer.ts", fileName);
    }

    [Fact]
    public void Emit_IncludesHeader()
    {
        // Arrange
        var model = TestDescriptorFactory.CreateModel("Calendar");

        // Act
        var result = TransformerEmitter.Emit(model);

        // Assert
        Assert.Contains("// ============================================", result);
        Assert.Contains("// Calendar Transformer", result);
    }
}
