using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using RepositoryFramework.UnitTest.TypescriptGenerator.Models;
using Xunit;
using static RepositoryFramework.UnitTest.TypescriptGenerator.TestDescriptorFactory;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Integration tests for TypeScriptGenerator - full generation pipeline.
/// </summary>
public class TypeScriptGeneratorTest : IDisposable
{
    private readonly string _testOutputDir;

    public TypeScriptGeneratorTest()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"rystem-ts-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testOutputDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputDir))
        {
            try
            {
                Directory.Delete(_testOutputDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    [Fact]
    public void Generate_SimpleModel_CreatesTypesDirectory()
    {
        // Arrange
        var model = CreateSimpleModel();
        var context = CreateContextForModels([model]);
        var generator = new TypeScriptGenerator(_testOutputDir, context, null, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert
        Assert.True(Directory.Exists(Path.Combine(_testOutputDir, "types")));
    }

    [Fact]
    public void Generate_SimpleModel_CreatesTypeFile()
    {
        // Arrange
        var model = CreateSimpleModel();
        var context = CreateContextForModels([model]);
        var generator = new TypeScriptGenerator(_testOutputDir, context, null, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "types", "user.ts");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Generate_SimpleModel_FileContainsInterface()
    {
        // Arrange
        var model = CreateSimpleModel();
        var context = CreateContextForModels([model]);
        var generator = new TypeScriptGenerator(_testOutputDir, context, null, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "types", "user.ts");
        var content = File.ReadAllText(filePath);
        Assert.Contains("export interface User", content);
    }

    [Fact]
    public void Generate_ModelWithRawType_ContainsRawInterface()
    {
        // Arrange
        var model = CreateModel("User",
            CreateProperty("FirstName", "fn", StringType));
        var context = CreateContextForModels([model]);
        var generator = new TypeScriptGenerator(_testOutputDir, context, null, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "types", "user.ts");
        var content = File.ReadAllText(filePath);
        Assert.Contains("export interface UserRaw", content);
        Assert.Contains("export interface User", content);
        Assert.Contains("fn: string", content);
        Assert.Contains("firstName: string", content);
    }

    [Fact]
    public void Generate_ModelWithRawType_ContainsMappers()
    {
        // Arrange
        var model = CreateModel("User",
            CreateProperty("Name", "n", StringType));
        var context = CreateContextForModels([model]);
        var generator = new TypeScriptGenerator(_testOutputDir, context, null, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "types", "user.ts");
        var content = File.ReadAllText(filePath);
        Assert.Contains("mapRawUserToUser", content);
        Assert.Contains("mapUserToRawUser", content);
    }

    [Fact]
    public void Generate_EnumModel_ContainsEnum()
    {
        // Arrange
        var enumModel = CreateEnum("Status", ("Active", 0), ("Inactive", 1));
        var context = CreateContextForModels([enumModel]);
        var generator = new TypeScriptGenerator(_testOutputDir, context, null, overwrite: true);

        // Act
        generator.Generate([enumModel], []);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "types", "status.ts");
        var content = File.ReadAllText(filePath);
        Assert.Contains("export enum Status", content);
        Assert.Contains("Active = 0", content);
        Assert.Contains("Inactive = 1", content);
    }

    [Fact]
    public void Generate_MultipleModels_CreatesMultipleFiles()
    {
        // Arrange
        var model1 = CreateModel("User", CreateProperty("Id", NumberType));
        var model2 = CreateModel("Order", CreateProperty("Id", NumberType));
        var context = CreateContextForModels([model1, model2]);
        var generator = new TypeScriptGenerator(_testOutputDir, context, null, overwrite: true);

        // Act
        generator.Generate([model1, model2], []);

        // Assert
        Assert.True(File.Exists(Path.Combine(_testOutputDir, "types", "user.ts")));
        Assert.True(File.Exists(Path.Combine(_testOutputDir, "types", "order.ts")));
    }

    [Fact]
    public void Generate_CreatesIndexFile()
    {
        // Arrange
        var model = CreateSimpleModel();
        var context = CreateContextForModels([model]);
        var generator = new TypeScriptGenerator(_testOutputDir, context, null, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert
        var indexPath = Path.Combine(_testOutputDir, "types", "index.ts");
        Assert.True(File.Exists(indexPath));
    }

    [Fact]
    public void Generate_IndexFile_ExportsModels()
    {
        // Arrange
        var model1 = CreateModel("User", CreateProperty("Id", NumberType));
        var model2 = CreateModel("Order", CreateProperty("Id", NumberType));
        var context = CreateContextForModels([model1, model2]);
        var generator = new TypeScriptGenerator(_testOutputDir, context, null, overwrite: true);

        // Act
        generator.Generate([model1, model2], []);

        // Assert
        var indexPath = Path.Combine(_testOutputDir, "types", "index.ts");
        var content = File.ReadAllText(indexPath);
        Assert.Contains("user", content.ToLower());
        Assert.Contains("order", content.ToLower());
    }

    [Fact]
    public void Generate_WithKeys_GeneratesKeyFiles()
    {
        // Arrange
        var model = CreateSimpleModel();
        var key = CreateModel("UserId", CreateProperty("Value", StringType));
        var context = CreateContextForModels([model, key]);
        var generator = new TypeScriptGenerator(_testOutputDir, context, null, overwrite: true);

        // Act
        generator.Generate([model], [key]);

        // Assert
        Assert.True(File.Exists(Path.Combine(_testOutputDir, "types", "user.ts")));
        Assert.True(File.Exists(Path.Combine(_testOutputDir, "types", "userid.ts")));
    }

    [Fact]
    public void Generate_FileContainsAutoGeneratedComment()
    {
        // Arrange
        var model = CreateSimpleModel();
        var context = CreateContextForModels([model]);
        var generator = new TypeScriptGenerator(_testOutputDir, context, null, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "types", "user.ts");
        var content = File.ReadAllText(filePath);
        Assert.Contains("Auto-generated by Rystem TypeScript Generator", content);
    }

    [Fact]
    public void Generate_WithArrayProperty_GeneratesArrayType()
    {
        // Arrange
        var model = CreateModel("Team",
            CreateProperty("Players", "players", CreateArrayType(StringType)));
        var context = CreateContextForModels([model]);
        var generator = new TypeScriptGenerator(_testOutputDir, context, null, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "types", "team.ts");
        var content = File.ReadAllText(filePath);
        Assert.Contains("players: string[]", content);
    }

    [Fact]
    public void Generate_WithHelperNeeded_GeneratesHelper()
    {
        // Arrange
        var model = CreateModel("Container",
            CreateProperty("Items", "items", CreateArrayType(StringType)));
        var context = CreateContextForModels([model]);
        var generator = new TypeScriptGenerator(_testOutputDir, context, null, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "types", "container.ts");
        var content = File.ReadAllText(filePath);
        Assert.Contains("ContainerHelper", content);
    }

    private static ModelDescriptor CreateSimpleModel()
    {
        return CreateModel("User",
            CreateProperty("Id", NumberType),
            CreateProperty("Name", StringType));
    }

    private static EmitterContext CreateContextForModels(List<ModelDescriptor> models)
    {
        return EmitterContext.FromAnalysisResult(models, [], []);
    }

    // ──────────────────────────────────────────
    // End-to-end: Enum/Flags generation from real C# types
    // ──────────────────────────────────────────

    [Fact]
    public void Generate_RealModel_EnumsAreEmittedInTypeScript()
    {
        // Arrange — analyze real TimelinePreview which has enums and flags
        var analyzer = new ModelAnalyzer();
        var model = analyzer.Analyze(typeof(TimelinePreview));
        var graph = new DependencyGraph();
        graph.AddModel(model);
        var ownership = graph.GetAllOwnership()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OwnerModel);
        var context = EmitterContext.FromAnalysisResult([model], [], ownership);
        var generator = new TypeScriptGenerator(_testOutputDir, context, graph, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert — the generated file must contain enum definitions
        var filePath = Path.Combine(_testOutputDir, "types", "timelinepreview.ts");
        Assert.True(File.Exists(filePath), "TimelinePreview TypeScript file should exist");
        var content = File.ReadAllText(filePath);

        // Regular enum
        Assert.Contains("export enum TimelineImportanceLevel", content);
        Assert.Contains("Level0 = 0", content);
        Assert.Contains("Level5 = 5", content);

        // Flags enum
        Assert.Contains("export enum SupportedCalendars", content);
        Assert.Contains("None = 0", content);
        Assert.Contains("Gregorian = 1", content);
        Assert.Contains("Japanese = 32", content);
    }

    [Fact]
    public void Generate_RealModel_DeeplyNestedEnumIsEmitted()
    {
        // Arrange — TimelineEventVisibility is only used by TimelineEventPreview
        // which is a nested type of TimelinePreview.
        // It should still be emitted in TimelinePreview's file.
        var analyzer = new ModelAnalyzer();
        var model = analyzer.Analyze(typeof(TimelinePreview));
        var graph = new DependencyGraph();
        graph.AddModel(model);
        var ownership = graph.GetAllOwnership()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OwnerModel);
        var context = EmitterContext.FromAnalysisResult([model], [], ownership);
        var generator = new TypeScriptGenerator(_testOutputDir, context, graph, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "types", "timelinepreview.ts");
        var content = File.ReadAllText(filePath);

        // Deeply nested enum
        Assert.Contains("export enum TimelineEventVisibility", content);
        Assert.Contains("Public = 0", content);
        Assert.Contains("Private = 1", content);
        Assert.Contains("Hidden = 2", content);
    }

    [Fact]
    public void Generate_RealModel_NestedClassIsEmitted()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var model = analyzer.Analyze(typeof(TimelinePreview));
        var graph = new DependencyGraph();
        graph.AddModel(model);
        var ownership = graph.GetAllOwnership()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OwnerModel);
        var context = EmitterContext.FromAnalysisResult([model], [], ownership);
        var generator = new TypeScriptGenerator(_testOutputDir, context, graph, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert — TimelineEventPreview should be in the file as a clean interface
        var filePath = Path.Combine(_testOutputDir, "types", "timelinepreview.ts");
        var content = File.ReadAllText(filePath);
        Assert.Contains("export interface TimelineEventPreview", content);
    }

    [Fact]
    public void Generate_RealModel_AnyOfRendersAsUnion()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var model = analyzer.Analyze(typeof(TimelinePreview));
        var graph = new DependencyGraph();
        graph.AddModel(model);
        var ownership = graph.GetAllOwnership()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OwnerModel);
        var context = EmitterContext.FromAnalysisResult([model], [], ownership);
        var generator = new TypeScriptGenerator(_testOutputDir, context, graph, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert — AnyOf<string, long> should be rendered as string | number
        var filePath = Path.Combine(_testOutputDir, "types", "timelinepreview.ts");
        var content = File.ReadAllText(filePath);
        Assert.Contains("string | number", content);
        Assert.DoesNotContain("AnyOf", content);
    }

    [Fact]
    public void Generate_TwoModels_SharedEnumIsDefinedInOwnerFile()
    {
        // Arrange — Both TimelinePreview and SearchResult use TimelineImportanceLevel.
        // When analyzed with the same ModelAnalyzer, the enum should be owned by
        // the first model (shallowest depth) and imported by the second.
        var analyzer = new ModelAnalyzer();
        var model1 = analyzer.Analyze(typeof(TimelinePreview));
        var model2 = analyzer.Analyze(typeof(SearchResult));
        var graph = new DependencyGraph();
        graph.AddModel(model1);
        graph.AddModel(model2);
        var ownership = graph.GetAllOwnership()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OwnerModel);
        var context = EmitterContext.FromAnalysisResult([model1, model2], [], ownership);
        var generator = new TypeScriptGenerator(_testOutputDir, context, graph, overwrite: true);

        // Act
        generator.Generate([model1, model2], []);

        // Assert — TimelineImportanceLevel should be defined in one file
        var file1 = Path.Combine(_testOutputDir, "types", "timelinepreview.ts");
        var file2 = Path.Combine(_testOutputDir, "types", "searchresult.ts");
        var content1 = File.ReadAllText(file1);
        var content2 = File.ReadAllText(file2);

        // Exactly one file should define the enum
        var defined1 = content1.Contains("export enum TimelineImportanceLevel");
        var defined2 = content2.Contains("export enum TimelineImportanceLevel");
        Assert.True(defined1 || defined2, "TimelineImportanceLevel should be defined in one of the files");

        // The other file should import it
        if (defined1)
        {
            Assert.Contains("import type", content2);
            Assert.Contains("TimelineImportanceLevel", content2);
        }
        else
        {
            Assert.Contains("import type", content1);
            Assert.Contains("TimelineImportanceLevel", content1);
        }
    }

    [Fact]
    public void Generate_TwoModels_SecondModelHasAllSharedEnumsInNestedTypes()
    {
        // Arrange — verify that after Bug 2 fix, both models have the shared enum in NestedTypes
        var analyzer = new ModelAnalyzer();
        var model1 = analyzer.Analyze(typeof(TimelinePreview));
        var model2 = analyzer.Analyze(typeof(SearchResult));

        // Assert — SearchResult should have TimelineImportanceLevel in its NestedTypes
        // even though TimelinePreview was analyzed first
        Assert.Contains(model2.NestedTypes, n => n.Name == "TimelineImportanceLevel");
    }

    // ──────────────────────────────────────────
    // GhostWriter-like scenarios: enum generation for direct properties
    // ──────────────────────────────────────────

    [Fact]
    public void Generate_GhostWriter_ParagraphPreviewHasFlagsEnum()
    {
        // Arrange — ParagraphPreview has ParagraphType [Flags] enum
        var analyzer = new ModelAnalyzer();
        var model = analyzer.Analyze(typeof(ParagraphPreview));
        var graph = new DependencyGraph();
        graph.AddModel(model);
        var ownership = graph.GetAllOwnership()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OwnerModel);
        var context = EmitterContext.FromAnalysisResult([model], [], ownership);
        var generator = new TypeScriptGenerator(_testOutputDir, context, graph, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "types", "paragraphpreview.ts");
        Assert.True(File.Exists(filePath), "ParagraphPreview TypeScript file should exist");
        var content = File.ReadAllText(filePath);

        // Flags enum must be generated in the same file
        Assert.Contains("export enum ParagraphType", content);
        Assert.Contains("None = 0", content);
        Assert.Contains("Descriptive = 1", content);
        Assert.Contains("Dialog = 2", content);
        Assert.Contains("Action = 4", content);
        Assert.Contains("Narration = 32", content);

        // Raw interface must reference the enum
        Assert.Contains("tp: ParagraphType", content);
    }

    [Fact]
    public void Generate_GhostWriter_LocationPreviewHasEnum()
    {
        // Arrange — LocationPreview has LocationType enum
        var analyzer = new ModelAnalyzer();
        var model = analyzer.Analyze(typeof(LocationPreview));
        var graph = new DependencyGraph();
        graph.AddModel(model);
        var ownership = graph.GetAllOwnership()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OwnerModel);
        var context = EmitterContext.FromAnalysisResult([model], [], ownership);
        var generator = new TypeScriptGenerator(_testOutputDir, context, graph, overwrite: true);

        // Act
        generator.Generate([model], []);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "types", "locationpreview.ts");
        Assert.True(File.Exists(filePath), "LocationPreview TypeScript file should exist");
        var content = File.ReadAllText(filePath);

        // Regular enum must be generated
        Assert.Contains("export enum LocationType", content);
        Assert.Contains("Main = 0", content);
        Assert.Contains("Other = 255", content);

        // Raw interface must reference the enum
        Assert.Contains("tp: LocationType", content);
    }

    // ──────────────────────────────────────────
    // ImportResolver fix: nested types' enum imports
    // ──────────────────────────────────────────

    [Fact]
    public void Generate_NestedTypeReferencesExternalEnum_ImportIsGenerated()
    {
        // Arrange — EventReport has a nested type ReportedEvent which
        // references TimelineEventVisibility and TimelineImportanceLevel enums.
        // These enums are owned by TimelinePreview (analyzed first).
        // EventReport's file must import them from timelinepreview.ts.
        var analyzer = new ModelAnalyzer();
        var timeline = analyzer.Analyze(typeof(TimelinePreview));
        var report = analyzer.Analyze(typeof(EventReport));
        var graph = new DependencyGraph();
        graph.AddModel(timeline);
        graph.AddModel(report);
        var ownership = graph.GetAllOwnership()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OwnerModel);
        var context = EmitterContext.FromAnalysisResult([timeline, report], [], ownership);
        var generator = new TypeScriptGenerator(_testOutputDir, context, graph, overwrite: true);

        // Act
        generator.Generate([timeline, report], []);

        // Assert — EventReport's file should import the enums from TimelinePreview's file
        var reportFile = Path.Combine(_testOutputDir, "types", "eventreport.ts");
        Assert.True(File.Exists(reportFile), "EventReport TypeScript file should exist");
        var reportContent = File.ReadAllText(reportFile);

        // Must contain imports for the enums
        Assert.Contains("import type", reportContent);
        Assert.Contains("TimelineEventVisibility", reportContent);
        Assert.Contains("TimelineImportanceLevel", reportContent);
        Assert.Contains("from './timelinepreview'", reportContent);

        // The enums should NOT be defined in EventReport's file (they're owned by TimelinePreview)
        Assert.DoesNotContain("export enum TimelineEventVisibility", reportContent);
        Assert.DoesNotContain("export enum TimelineImportanceLevel", reportContent);

        // The enums SHOULD be defined in TimelinePreview's file
        var timelineFile = Path.Combine(_testOutputDir, "types", "timelinepreview.ts");
        var timelineContent = File.ReadAllText(timelineFile);
        Assert.Contains("export enum TimelineEventVisibility", timelineContent);
        Assert.Contains("export enum TimelineImportanceLevel", timelineContent);
    }

    [Fact]
    public void Generate_TwoRootModels_BothGetEnumsInOwnFile()
    {
        // Arrange — ParagraphPreview and LocationPreview are independent root models
        // each with their own enum. Both should have their enums in their own files.
        var analyzer = new ModelAnalyzer();
        var para = analyzer.Analyze(typeof(ParagraphPreview));
        var loc = analyzer.Analyze(typeof(LocationPreview));
        var graph = new DependencyGraph();
        graph.AddModel(para);
        graph.AddModel(loc);
        var ownership = graph.GetAllOwnership()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OwnerModel);
        var context = EmitterContext.FromAnalysisResult([para, loc], [], ownership);
        var generator = new TypeScriptGenerator(_testOutputDir, context, graph, overwrite: true);

        // Act
        generator.Generate([para, loc], []);

        // Assert — each file should contain its own enum
        var paraContent = File.ReadAllText(Path.Combine(_testOutputDir, "types", "paragraphpreview.ts"));
        var locContent = File.ReadAllText(Path.Combine(_testOutputDir, "types", "locationpreview.ts"));

        Assert.Contains("export enum ParagraphType", paraContent);
        Assert.DoesNotContain("export enum LocationType", paraContent);

        Assert.Contains("export enum LocationType", locContent);
        Assert.DoesNotContain("export enum ParagraphType", locContent);
    }
}
