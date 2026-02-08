using System;
using System.Collections.Generic;
using System.Linq;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using RepositoryFramework.UnitTest.TypescriptGenerator.Models;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for AnyOf discriminated union support and deep nested type discovery.
/// </summary>
public class AnyOfUnionTest
{
    private readonly TypeResolver _resolver = new();
    private readonly ModelAnalyzer _analyzer = new();

    // ──────────────────────────────────────────
    // TypeResolver: AnyOf → union type
    // ──────────────────────────────────────────

    [Fact]
    public void Resolve_AnyOfStringLong_ReturnsUnionType()
    {
        // Act
        var result = _resolver.Resolve(typeof(AnyOf<string, long>));

        // Assert
        Assert.True(result.IsUnion);
        Assert.False(result.IsPrimitive);
        Assert.False(result.IsArray);
        Assert.False(result.IsDictionary);
        Assert.Equal("string | number", result.TypeScriptName);
        Assert.NotNull(result.UnionTypes);
        Assert.Equal(2, result.UnionTypes!.Count);
    }

    [Fact]
    public void Resolve_AnyOfStringInt_UnionMembersAreCorrect()
    {
        // Act
        var result = _resolver.Resolve(typeof(AnyOf<string, int>));

        // Assert
        Assert.Equal("string", result.UnionTypes![0].TypeScriptName);
        Assert.True(result.UnionTypes[0].IsPrimitive);
        Assert.Equal("number", result.UnionTypes[1].TypeScriptName);
        Assert.True(result.UnionTypes[1].IsPrimitive);
    }

    [Fact]
    public void Resolve_AnyOfWithComplexType_IncludesComplexTypeName()
    {
        // AnyOf<TimelineEventPreview, string> → "TimelineEventPreview | string"
        var result = _resolver.Resolve(typeof(AnyOf<TimelineEventPreview, string>));

        Assert.True(result.IsUnion);
        Assert.Equal("TimelineEventPreview | string", result.TypeScriptName);

        // First member is complex, second is primitive
        Assert.False(result.UnionTypes![0].IsPrimitive);
        Assert.Equal("TimelineEventPreview", result.UnionTypes[0].TypeScriptName);
        Assert.True(result.UnionTypes[1].IsPrimitive);
    }

    [Fact]
    public void Resolve_AnyOfWithEnum_IncludesEnumTypeName()
    {
        // AnyOf<TimelineImportanceLevel, int> → "TimelineImportanceLevel | number"
        var result = _resolver.Resolve(typeof(AnyOf<TimelineImportanceLevel, int>));

        Assert.True(result.IsUnion);
        Assert.Equal("TimelineImportanceLevel | number", result.TypeScriptName);
        Assert.True(result.UnionTypes![0].IsEnum);
    }

    [Fact]
    public void Resolve_AnyOfThreeTypes_AllIncluded()
    {
        // AnyOf<string, int, bool> → "string | number | boolean"
        var result = _resolver.Resolve(typeof(AnyOf<string, int, bool>));

        Assert.True(result.IsUnion);
        Assert.Equal("string | number | boolean", result.TypeScriptName);
        Assert.Equal(3, result.UnionTypes!.Count);
    }

    // ──────────────────────────────────────────
    // ModelAnalyzer: AnyOf NOT analyzed as model
    // ──────────────────────────────────────────

    [Fact]
    public void Analyze_TimelinePreview_AnyOfNotInAnalyzedModels()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        _analyzer.Analyze(typeof(TimelinePreview));

        // Assert — AnyOf itself should NOT be an analyzed model
        var models = _analyzer.GetAnalyzedModels();
        Assert.DoesNotContain(models.Values, m => m.Name.Contains("AnyOf"));
    }

    [Fact]
    public void Analyze_TimelinePreview_FromAndToAreUnionTypes()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        var descriptor = _analyzer.Analyze(typeof(TimelinePreview));

        // Assert
        var fromProp = descriptor.Properties.First(p => p.CSharpName == "From");
        Assert.True(fromProp.Type.IsUnion);
        Assert.Equal("string | number", fromProp.Type.TypeScriptName);

        var toProp = descriptor.Properties.First(p => p.CSharpName == "To");
        Assert.True(toProp.Type.IsUnion);
        Assert.Equal("string | number", toProp.Type.TypeScriptName);
    }

    // ──────────────────────────────────────────
    // Deep nested type discovery
    // ──────────────────────────────────────────

    [Fact]
    public void Analyze_TimelinePreview_DiscoversAllEnums()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        _analyzer.Analyze(typeof(TimelinePreview));

        // Assert — all enums should be discovered
        var models = _analyzer.GetAnalyzedModels();
        Assert.Contains(models.Values, m => m.Name == "TimelineImportanceLevel" && m.IsEnum);
        Assert.Contains(models.Values, m => m.Name == "SupportedCalendars" && m.IsEnum);
    }

    [Fact]
    public void Analyze_TimelinePreview_DiscoversFlagsEnum()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        _analyzer.Analyze(typeof(TimelinePreview));

        // Assert — SupportedCalendars is [Flags], should still be an enum
        var models = _analyzer.GetAnalyzedModels();
        var flagsEnum = models.Values.FirstOrDefault(m => m.Name == "SupportedCalendars");
        Assert.NotNull(flagsEnum);
        Assert.True(flagsEnum!.IsEnum);
        Assert.NotNull(flagsEnum.EnumValues);
        Assert.Contains(flagsEnum.EnumValues!, v => v.Name == "Gregorian" && v.Value == 1);
        Assert.Contains(flagsEnum.EnumValues!, v => v.Name == "Japanese" && v.Value == 32);
    }

    [Fact]
    public void Analyze_TimelinePreview_DiscoversNestedClassAndItsEnums()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        _analyzer.Analyze(typeof(TimelinePreview));

        // Assert — TimelineEventPreview and its enum should be discovered
        var models = _analyzer.GetAnalyzedModels();
        Assert.Contains(models.Values, m => m.Name == "TimelineEventPreview");
        Assert.Contains(models.Values, m => m.Name == "TimelineEventVisibility" && m.IsEnum);
    }

    [Fact]
    public void Analyze_SearchResult_AnyOfWithComplexTypeDiscoversNestedType()
    {
        // Arrange — SearchResult has AnyOf<TimelineEventPreview, string>
        _analyzer.Clear();

        // Act
        _analyzer.Analyze(typeof(SearchResult));

        // Assert — TimelineEventPreview should be discovered through the AnyOf union member
        var models = _analyzer.GetAnalyzedModels();
        Assert.Contains(models.Values, m => m.Name == "TimelineEventPreview");
        // And its nested enum too
        Assert.Contains(models.Values, m => m.Name == "TimelineEventVisibility");
        // AnyOf itself should NOT be a model
        Assert.DoesNotContain(models.Values, m => m.Name.Contains("AnyOf"));
    }

    [Fact]
    public void Analyze_SearchResult_AnyOfWithEnumDiscoversEnum()
    {
        // Arrange — SearchResult has AnyOf<TimelineImportanceLevel, int>
        _analyzer.Clear();

        // Act
        _analyzer.Analyze(typeof(SearchResult));

        // Assert — TimelineImportanceLevel should be discovered through the AnyOf union member
        var models = _analyzer.GetAnalyzedModels();
        Assert.Contains(models.Values, m => m.Name == "TimelineImportanceLevel" && m.IsEnum);
    }

    // ──────────────────────────────────────────
    // End-to-end: Emitter output verification
    // ──────────────────────────────────────────

    [Fact]
    public void Emit_FlagsEnum_GeneratesCorrectBitValues()
    {
        // Arrange
        _analyzer.Clear();
        _analyzer.Analyze(typeof(TimelinePreview));
        var models = _analyzer.GetAnalyzedModels();
        var flagsEnum = models.Values.First(m => m.Name == "SupportedCalendars");

        // Act
        var result = EnumEmitter.Emit(flagsEnum);

        // Assert — flags enum should have bit-shifted values
        Assert.Contains("export enum SupportedCalendars {", result);
        Assert.Contains("None = 0", result);
        Assert.Contains("Gregorian = 1", result);
        Assert.Contains("Julian = 2", result);
        Assert.Contains("Hebrew = 4", result);
        Assert.Contains("Hijri = 8", result);
        Assert.Contains("ChineseLunisolar = 16", result);
        Assert.Contains("Japanese = 32", result);
    }

    [Fact]
    public void Emit_RegularEnum_GeneratesCorrectValues()
    {
        // Arrange
        _analyzer.Clear();
        _analyzer.Analyze(typeof(TimelinePreview));
        var models = _analyzer.GetAnalyzedModels();
        var enumModel = models.Values.First(m => m.Name == "TimelineImportanceLevel");

        // Act
        var result = EnumEmitter.Emit(enumModel);

        // Assert
        Assert.Contains("export enum TimelineImportanceLevel {", result);
        Assert.Contains("Level0 = 0", result);
        Assert.Contains("Level5 = 5", result);
    }

    [Fact]
    public void Emit_CleanInterface_AnyOfRendersAsUnion()
    {
        // Arrange
        _analyzer.Clear();
        var model = _analyzer.Analyze(typeof(TimelinePreview));
        var context = CreateEmitterContext(model);

        // Act
        var result = CleanTypeEmitter.Emit(model, context);

        // Assert — AnyOf<string, long> should render as "string | number"
        Assert.Contains("string | number", result);
        // "from" and "to" should be optional union properties
        Assert.Contains("from?:", result);
        Assert.Contains("to?:", result);
        // Should NOT contain "AnyOf"
        Assert.DoesNotContain("AnyOf", result);
    }

    [Fact]
    public void Emit_RawInterface_AnyOfRendersAsUnion()
    {
        // Arrange
        _analyzer.Clear();
        var model = _analyzer.Analyze(typeof(TimelinePreview));
        var context = CreateEmitterContext(model);

        // Act
        var result = RawTypeEmitter.Emit(model, context);

        // Assert — In raw interface, should also render union
        Assert.Contains("string | number", result);
        Assert.DoesNotContain("AnyOf", result);
    }

    [Fact]
    public void Emit_CleanInterface_EnumPropertiesUseEnumTypeName()
    {
        // Arrange
        _analyzer.Clear();
        var model = _analyzer.Analyze(typeof(TimelinePreview));
        var context = CreateEmitterContext(model);

        // Act
        var result = CleanTypeEmitter.Emit(model, context);

        // Assert — enum properties should use enum type name
        Assert.Contains("TimelineImportanceLevel", result);
        Assert.Contains("SupportedCalendars", result);
    }

    [Fact]
    public void Emit_CleanInterface_NestedArrayUsesCorrectType()
    {
        // Arrange
        _analyzer.Clear();
        var model = _analyzer.Analyze(typeof(TimelinePreview));
        var context = CreateEmitterContext(model);

        // Act
        var result = CleanTypeEmitter.Emit(model, context);

        // Assert — List<TimelineEventPreview> should render as TimelineEventPreview[]
        Assert.Contains("TimelineEventPreview[]", result);
    }

    [Fact]
    public void Emit_NestedClassEnums_GenerateCorrectly()
    {
        // Arrange
        _analyzer.Clear();
        _analyzer.Analyze(typeof(TimelinePreview));
        var models = _analyzer.GetAnalyzedModels();
        var eventPreview = models.Values.First(m => m.Name == "TimelineEventPreview");
        var context = CreateEmitterContext(eventPreview);

        // Act
        var result = CleanTypeEmitter.Emit(eventPreview, context);

        // Assert — TimelineEventPreview should reference its own enum
        Assert.Contains("TimelineEventVisibility", result);
        Assert.Contains("TimelineImportanceLevel", result);
    }

    [Fact]
    public void Emit_MapperWithUnion_IsPassThrough()
    {
        // Arrange
        _analyzer.Clear();
        var model = _analyzer.Analyze(typeof(TimelinePreview));
        var context = CreateEmitterContext(model);

        // Act
        var result = MapperEmitter.Emit(model, context);

        // Assert — union properties should be pass-through (no mapper function call)
        // The mapper should exist since TimelinePreview has JsonPropertyName attributes
        Assert.NotEmpty(result);
        // from/to with raw access should be direct: raw.f and raw.t
        Assert.Contains("raw.f", result);
        Assert.Contains("raw.t", result);
    }

    // ──────────────────────────────────────────
    // Helper to create EmitterContext
    // ──────────────────────────────────────────

    private static EmitterContext CreateEmitterContext(ModelDescriptor model)
    {
        var ownership = new Dictionary<string, string>();
        ownership[model.Name] = model.Name;
        CollectOwnership(model, model.Name, ownership);

        return EmitterContext.FromAnalysisResult(
            [model],
            Array.Empty<ModelDescriptor>(),
            ownership);
    }

    private static void CollectOwnership(ModelDescriptor model, string ownerName, Dictionary<string, string> ownership)
    {
        foreach (var nested in model.NestedTypes)
        {
            if (!ownership.ContainsKey(nested.Name))
            {
                ownership[nested.Name] = ownerName;
                CollectOwnership(nested, ownerName, ownership);
            }
        }
    }
}
