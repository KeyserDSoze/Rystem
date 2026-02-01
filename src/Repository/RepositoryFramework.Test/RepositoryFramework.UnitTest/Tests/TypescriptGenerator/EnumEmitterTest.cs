using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using Xunit;
using static RepositoryFramework.UnitTest.TypescriptGenerator.TestDescriptorFactory;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for EnumEmitter - generating TypeScript enums from C# enums.
/// </summary>
public class EnumEmitterTest
{
    [Fact]
    public void Emit_SimpleEnum_GeneratesCorrectTypeScript()
    {
        // Arrange
        var enumDescriptor = CreateEnum("GameStatus", 
            ("NotStarted", 0), ("InProgress", 1), ("Completed", 2));

        // Act
        var result = EnumEmitter.Emit(enumDescriptor);

        // Assert
        Assert.Contains("export enum GameStatus {", result);
        Assert.Contains("NotStarted = 0", result);
        Assert.Contains("InProgress = 1", result);
        Assert.Contains("Completed = 2", result);
    }

    [Fact]
    public void Emit_EnumWithCustomValues_GeneratesCorrectValues()
    {
        // Arrange
        var enumDescriptor = CreateEnum("Priority", 
            ("Low", 10), ("Normal", 20), ("High", 100));

        // Act
        var result = EnumEmitter.Emit(enumDescriptor);

        // Assert
        Assert.Contains("Low = 10", result);
        Assert.Contains("Normal = 20", result);
        Assert.Contains("High = 100", result);
    }

    [Fact]
    public void Emit_SingleValueEnum_GeneratesWithoutTrailingComma()
    {
        // Arrange
        var enumDescriptor = CreateEnum("SingleValue", ("Only", 0));

        // Act
        var result = EnumEmitter.Emit(enumDescriptor);

        // Assert
        Assert.Contains("Only = 0", result);
        Assert.DoesNotContain("Only = 0,", result);
    }

    [Fact]
    public void Emit_NonEnumModel_ThrowsArgumentException()
    {
        // Arrange
        var nonEnumDescriptor = CreateModel("NotAnEnum");

        // Act & Assert
        Assert.Throws<System.ArgumentException>(() => EnumEmitter.Emit(nonEnumDescriptor));
    }

    [Fact]
    public void EmitAll_MultipleEnums_GeneratesAllEnums()
    {
        // Arrange
        var enums = new[]
        {
            CreateEnum("Status", ("Active", 0), ("Inactive", 1)),
            CreateEnum("Level", ("Beginner", 1), ("Advanced", 2)),
            CreateModel("NotAnEnum")
        };

        // Act
        var result = EnumEmitter.EmitAll(enums);

        // Assert
        Assert.Contains("export enum Status", result);
        Assert.Contains("export enum Level", result);
        Assert.DoesNotContain("NotAnEnum", result);
    }

    [Fact]
    public void Emit_EnumFormat_HasCorrectIndentation()
    {
        // Arrange
        var enumDescriptor = CreateEnum("Color", ("Red", 0), ("Green", 1));

        // Act
        var result = EnumEmitter.Emit(enumDescriptor);
        var lines = result.Split('\n');

        // Assert - check indentation with 2 spaces
        Assert.Contains(lines, l => l.TrimEnd() == "  Red = 0,");
        Assert.Contains(lines, l => l.TrimEnd() == "  Green = 1");
    }
}
