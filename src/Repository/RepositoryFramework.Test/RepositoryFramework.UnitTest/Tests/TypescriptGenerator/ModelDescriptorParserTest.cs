using System;
using RepositoryFramework.Tools.TypescriptGenerator.Cli;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for ModelDescriptorParser - parsing CLI arguments.
/// </summary>
public class ModelDescriptorParserTest
{
    [Fact]
    public void Parse_SingleRepository_ReturnsCorrectDescriptor()
    {
        // Arrange
        var input = "[{Calendar,LeagueKey,Repository,Calendar}]";

        // Act
        var result = ModelDescriptorParser.Parse(input);

        // Assert
        Assert.Single(result);
        var descriptor = result[0];
        Assert.Equal("Calendar", descriptor.ModelName);
        Assert.Equal("LeagueKey", descriptor.KeyName);
        Assert.Equal(RepositoryKind.Repository, descriptor.Kind);
        Assert.Equal("Calendar", descriptor.FactoryName);
        Assert.False(descriptor.IsPrimitiveKey);
    }

    [Fact]
    public void Parse_MultipleRepositories_ReturnsAllDescriptors()
    {
        // Arrange
        var input = "[{Calendar,LeagueKey,Repository,Calendar},{Rank,LeagueKey,Repository,Rank},{Team,string,Query,Team}]";

        // Act
        var result = ModelDescriptorParser.Parse(input);

        // Assert
        Assert.Equal(3, result.Count);

        Assert.Equal("Calendar", result[0].ModelName);
        Assert.Equal("LeagueKey", result[0].KeyName);
        Assert.Equal(RepositoryKind.Repository, result[0].Kind);

        Assert.Equal("Rank", result[1].ModelName);
        Assert.Equal("LeagueKey", result[1].KeyName);
        Assert.Equal(RepositoryKind.Repository, result[1].Kind);

        Assert.Equal("Team", result[2].ModelName);
        Assert.Equal("string", result[2].KeyName);
        Assert.Equal(RepositoryKind.Query, result[2].Kind);
        Assert.True(result[2].IsPrimitiveKey);
    }

    [Fact]
    public void Parse_CommandKind_ParsesCorrectly()
    {
        // Arrange
        var input = "[{User,Guid,Command,UserWrite}]";

        // Act
        var result = ModelDescriptorParser.Parse(input);

        // Assert
        Assert.Single(result);
        Assert.Equal(RepositoryKind.Command, result[0].Kind);
        Assert.Equal("UserWrite", result[0].FactoryName);
        Assert.True(result[0].IsPrimitiveKey);
    }

    [Fact]
    public void Parse_WithoutFactoryName_UsesModelNameAsDefault()
    {
        // Arrange
        var input = "[{Calendar,LeagueKey,Repository}]";

        // Act
        var result = ModelDescriptorParser.Parse(input);

        // Assert
        Assert.Single(result);
        Assert.Equal("Calendar", result[0].FactoryName);
    }

    [Fact]
    public void Parse_PrimitiveKeys_IdentifiedCorrectly()
    {
        // Arrange & Act & Assert
        var stringKey = ModelDescriptorParser.Parse("[{Model,string,Repository}]")[0];
        Assert.True(stringKey.IsPrimitiveKey);

        var intKey = ModelDescriptorParser.Parse("[{Model,int,Repository}]")[0];
        Assert.True(intKey.IsPrimitiveKey);

        var guidKey = ModelDescriptorParser.Parse("[{Model,Guid,Repository}]")[0];
        Assert.True(guidKey.IsPrimitiveKey);

        var longKey = ModelDescriptorParser.Parse("[{Model,long,Repository}]")[0];
        Assert.True(longKey.IsPrimitiveKey);

        var customKey = ModelDescriptorParser.Parse("[{Model,CustomKey,Repository}]")[0];
        Assert.False(customKey.IsPrimitiveKey);
    }

    [Fact]
    public void Parse_CaseInsensitiveKind_ParsesCorrectly()
    {
        // Arrange & Act
        var repo1 = ModelDescriptorParser.Parse("[{Model,Key,repository}]")[0];
        var repo2 = ModelDescriptorParser.Parse("[{Model,Key,REPOSITORY}]")[0];
        var query = ModelDescriptorParser.Parse("[{Model,Key,query}]")[0];
        var command = ModelDescriptorParser.Parse("[{Model,Key,COMMAND}]")[0];

        // Assert
        Assert.Equal(RepositoryKind.Repository, repo1.Kind);
        Assert.Equal(RepositoryKind.Repository, repo2.Kind);
        Assert.Equal(RepositoryKind.Query, query.Kind);
        Assert.Equal(RepositoryKind.Command, command.Kind);
    }

    [Fact]
    public void Parse_EmptyInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ModelDescriptorParser.Parse(""));
        Assert.Throws<ArgumentException>(() => ModelDescriptorParser.Parse("   "));
    }

    [Fact]
    public void Parse_NewFormatSingleRepository_ReturnsCorrectDescriptor()
    {
        // Arrange - New format without square brackets
        var input = "{Calendar,LeagueKey,Repository,Calendar}";

        // Act
        var result = ModelDescriptorParser.Parse(input);

        // Assert
        Assert.Single(result);
        var descriptor = result[0];
        Assert.Equal("Calendar", descriptor.ModelName);
        Assert.Equal("LeagueKey", descriptor.KeyName);
        Assert.Equal(RepositoryKind.Repository, descriptor.Kind);
        Assert.Equal("Calendar", descriptor.FactoryName);
    }

    [Fact]
    public void Parse_NewFormatMultipleRepositories_ReturnsAllDescriptors()
    {
        // Arrange - New format: "{...},{...}"
        var input = "{Rank,RankKey,Repository,rank},{Team,Guid,Query,teams}";

        // Act
        var result = ModelDescriptorParser.Parse(input);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Rank", result[0].ModelName);
        Assert.Equal("rank", result[0].FactoryName);
        Assert.Equal("Team", result[1].ModelName);
        Assert.Equal("teams", result[1].FactoryName);
    }

    [Fact]
    public void Parse_LegacyFormatStillWorks()
    {
        // Arrange - Legacy format with square brackets
        var input = "[{Calendar,LeagueKey,Repository,Calendar}]";

        // Act
        var result = ModelDescriptorParser.Parse(input);

        // Assert
        Assert.Single(result);
        Assert.Equal("Calendar", result[0].ModelName);
    }

    [Fact]
    public void Parse_InvalidFormat_ThrowsArgumentException()
    {
        // These should fail - not starting with { or [
        Assert.Throws<ArgumentException>(() => ModelDescriptorParser.Parse("Model,Key,Repository"));
        Assert.Throws<ArgumentException>(() => ModelDescriptorParser.Parse("(Model,Key,Repository)"));
    }

    [Fact]
    public void Parse_InvalidKind_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ModelDescriptorParser.Parse("[{Model,Key,InvalidKind}]"));

        Assert.Contains("InvalidKind", ex.Message);
        Assert.Contains("Repository", ex.Message); // Should suggest valid values
    }

    [Fact]
    public void Parse_TooFewParts_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ModelDescriptorParser.Parse("[{Model,Key}]"));
    }

    [Fact]
    public void Parse_TooManyParts_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ModelDescriptorParser.Parse("[{Model,Key,Repository,Factory,Extra}]"));
    }

    [Fact]
    public void Parse_WithSpaces_TrimsCorrectly()
    {
        // Arrange
        var input = "[ { Calendar , LeagueKey , Repository , Calendar } ]";

        // Act
        var result = ModelDescriptorParser.Parse(input);

        // Assert
        Assert.Single(result);
        Assert.Equal("Calendar", result[0].ModelName);
        Assert.Equal("LeagueKey", result[0].KeyName);
    }

    [Fact]
    public void Validate_ValidInput_ReturnsTrue()
    {
        // Arrange
        var input = "[{Calendar,LeagueKey,Repository,Calendar}]";

        // Act
        var (isValid, errorMessage) = ModelDescriptorParser.Validate(input);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void Validate_InvalidInput_ReturnsFalseWithMessage()
    {
        // Arrange
        var input = "[{Model,Key}]"; // Missing Kind

        // Act
        var (isValid, errorMessage) = ModelDescriptorParser.Validate(input);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
    }
}
