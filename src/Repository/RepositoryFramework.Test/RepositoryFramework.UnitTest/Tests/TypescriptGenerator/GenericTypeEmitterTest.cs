using System;
using System.Collections.Generic;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using RepositoryFramework.UnitTest.TypescriptGenerator.Models;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for generic type generation in TypeScript.
/// </summary>
public class GenericTypeEmitterTest
{
    [Fact]
    public void Emit_GenericTypeWithSingleParameter_GeneratesCorrectInterface()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var openGenericType = typeof(EntityVersions<>);
        var model = analyzer.Analyze(openGenericType);

        var context = EmitterContext.FromAnalysisResult(
            models: [model],
            keys: [],
            typeOwnership: new Dictionary<string, string>());

        // Act - Generate Clean interface
        var cleanOutput = CleanTypeEmitter.Emit(model, context);

        // Debug output
        var output = new System.Text.StringBuilder();
        output.AppendLine($"Model Name: {model.Name}");
        output.AppendLine($"Is Generic: {model.IsGenericType}");
        output.AppendLine($"Generic Parameters: {string.Join(", ", model.GenericTypeParameters)}");
        output.AppendLine($"\nProperties:");
        foreach (var prop in model.Properties)
        {
            output.AppendLine($"  - {prop.TypeScriptName}: {prop.Type.TypeScriptName} (Optional: {prop.IsOptional})");
        }
        output.AppendLine($"\nGenerated Clean Output:\n{cleanOutput}");

        // Assert with better message
        if (!cleanOutput.Contains("versions?: VersionEntry<T>[]"))
        {
            throw new Exception($"Generated output doesn't contain expected generic property.\n\n{output}");
        }

        Assert.Contains("export interface EntityVersions<T> {", cleanOutput);
        Assert.Contains("current?: T", cleanOutput);
        Assert.Contains("versions?: VersionEntry<T>[]", cleanOutput);
    }

    [Fact]
    public void Emit_GenericTypeWithSingleParameter_GeneratesCorrectRawInterface()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var openGenericType = typeof(EntityVersions<>);
        var model = analyzer.Analyze(openGenericType);

        var context = EmitterContext.FromAnalysisResult(
            models: [model],
            keys: [],
            typeOwnership: new Dictionary<string, string>());

        // Act - Generate Raw interface
        var rawOutput = RawTypeEmitter.Emit(model, context);

        // Debug output
        var output = new System.Text.StringBuilder();
        output.AppendLine($"Generated Raw Output:\n{rawOutput}");

        // Assert with better message
        if (!rawOutput.Contains("versions?: VersionEntryRaw<T>[]"))
        {
            throw new Exception($"Generated output doesn't contain expected generic Raw property.\n\n{output}");
        }

        Assert.Contains("export interface EntityVersionsRaw<T> {", rawOutput);
        Assert.Contains("current?: T", rawOutput);
        Assert.Contains("versions?: VersionEntryRaw<T>[]", rawOutput);
        Assert.Contains("entity_id?: string", rawOutput); // JSON property name
    }

    [Fact]
    public void Analyze_ClosedGenericType_ReferencesOpenGeneric()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var closedGenericType = typeof(EntityVersions<Timeline>);

        // Act
        var model = analyzer.Analyze(closedGenericType);

        // Assert
        Assert.NotNull(model.GenericBaseTypeName);
        Assert.Equal("EntityVersions", model.GenericBaseTypeName); // Should reference base type without generic parameters
    }

    [Fact]
    public void Analyze_OpenGenericType_HasGenericParameters()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var openGenericType = typeof(EntityVersions<>);

        // Act
        var model = analyzer.Analyze(openGenericType);

        // Assert
        Assert.True(model.IsGenericType);
        Assert.Single(model.GenericTypeParameters);
        Assert.Equal("T", model.GenericTypeParameters[0]);
    }

    [Fact]
    public void Analyze_NestedGenericType_HasGenericParameters()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var openGenericType = typeof(VersionEntry<>);

        // Act
        var model = analyzer.Analyze(openGenericType);

        // Assert
        Assert.True(model.IsGenericType);
        Assert.Single(model.GenericTypeParameters);
        Assert.Equal("T", model.GenericTypeParameters[0]);
    }

    [Fact]
    public void GetFileName_GenericType_UsesBaseNameWithoutBacktick()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var openGenericType = typeof(EntityVersions<>);
        var model = analyzer.Analyze(openGenericType);

        // Act
        var fileName = model.GetFileName();

        // Assert
        Assert.Equal("entityversions.ts", fileName);
    }

    [Fact]
    public void GetCleanTypeName_GenericType_IncludesTypeParameter()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var openGenericType = typeof(EntityVersions<>);
        var model = analyzer.Analyze(openGenericType);

        // Act
        var cleanName = model.GetCleanTypeName();

        // Assert
        Assert.Equal("EntityVersions<T>", cleanName);
    }

    [Fact]
    public void GetRawTypeName_GenericType_IncludesTypeParameterAndRawSuffix()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var openGenericType = typeof(EntityVersions<>);
        var model = analyzer.Analyze(openGenericType);

        // Act
        var rawName = model.GetRawTypeName();

        // Assert
        Assert.Equal("EntityVersions<T>Raw", rawName);
    }
}
