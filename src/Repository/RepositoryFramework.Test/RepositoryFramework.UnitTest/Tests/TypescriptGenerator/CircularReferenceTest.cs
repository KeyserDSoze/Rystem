using System;
using System.Collections.Generic;
using System.Linq;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using RepositoryFramework.UnitTest.TypescriptGenerator.Models;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for circular reference handling in ModelAnalyzer.
/// These tests verify that stack overflow is prevented when models reference each other.
/// </summary>
public class CircularReferenceTest
{
    [Fact]
    public void Analyze_BookWithEntityVersionsOfBook_DoesNotStackOverflow()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();

        // Act - This used to cause a stack overflow
        var model = analyzer.Analyze(typeof(Book));

        // Assert
        Assert.NotNull(model);
        Assert.Equal("Book", model.Name);
        Assert.Contains(model.Properties, p => p.CSharpName == "History");
        
        // Verify that the circular reference was handled
        var historyProp = model.Properties.First(p => p.CSharpName == "History");
        Assert.Equal("EntityVersions<Book>", historyProp.Type.TypeScriptName);
    }

    [Fact]
    public void Analyze_EntityVersionsOfBook_DoesNotStackOverflow()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();

        // Act - Analyze the generic type directly
        var closedGenericType = typeof(EntityVersions<Book>);
        var model = analyzer.Analyze(closedGenericType);

        // Assert
        Assert.NotNull(model);
        // The closed generic should trigger analysis of the open generic
        Assert.Contains(model.NestedTypes, nt => nt.Name.Contains("EntityVersions"));
    }

    [Fact]
    public void Analyze_TreeNodeWithSelfReference_DoesNotStackOverflow()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();

        // Act - Direct self-reference
        var model = analyzer.Analyze(typeof(TreeNode));

        // Assert
        Assert.NotNull(model);
        Assert.Equal("TreeNode", model.Name);
        Assert.Contains(model.Properties, p => p.CSharpName == "Children");
        Assert.Contains(model.Properties, p => p.CSharpName == "Parent");
    }

    [Fact]
    public void Analyze_MutualCircularReference_DoesNotStackOverflow()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();

        // Act - A -> B -> A
        var modelA = analyzer.Analyze(typeof(PersonA));
        var modelB = analyzer.Analyze(typeof(PersonB));

        // Assert
        Assert.NotNull(modelA);
        Assert.NotNull(modelB);
        Assert.Equal("PersonA", modelA.Name);
        Assert.Equal("PersonB", modelB.Name);
        
        // Verify mutual references exist
        Assert.Contains(modelA.Properties, p => p.Type.CSharpName == "PersonB");
        Assert.Contains(modelB.Properties, p => p.Type.CSharpName == "PersonA");
    }

    [Fact]
    public void Emit_CircularReferenceModel_GeneratesValidTypeScript()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var model = analyzer.Analyze(typeof(Book));

        var context = EmitterContext.FromAnalysisResult(
            models: [model],
            keys: [],
            typeOwnership: new Dictionary<string, string>());

        // Act
        var output = CleanTypeEmitter.Emit(model, context);

        // Assert - Should generate valid TypeScript without stack overflow
        Assert.Contains("export interface Book {", output);
        Assert.Contains("history?: EntityVersions<Book>", output);
    }

    [Fact]
    public void Analyze_DeepNestedCircularReferences_DoesNotStackOverflow()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();

        // Act - Test multiple levels: EntityVersions<Book> where Book has EntityVersions<Book>
        var model = analyzer.Analyze(typeof(EntityVersions<Book>));

        // Assert - Should complete without stack overflow
        Assert.NotNull(model);
        
        // Verify that Book was discovered as a nested type
        var allDiscoveredTypes = model.NestedTypes
            .Concat(model.NestedTypes.SelectMany(nt => nt.NestedTypes))
            .ToList();
        
        Assert.Contains(allDiscoveredTypes, nt => nt.Name == "Book");
    }
}
