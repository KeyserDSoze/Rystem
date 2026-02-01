using System;
using System.Collections.Generic;
using System.IO;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
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
}
