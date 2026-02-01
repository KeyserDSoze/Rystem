using RepositoryFramework.Tools.TypescriptGenerator.Utils;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for string extension methods.
/// </summary>
public class StringExtensionsTest
{
    [Theory]
    [InlineData("UserProfile", "userProfile")]
    [InlineData("Calendar", "calendar")]
    [InlineData("ID", "iD")]
    [InlineData("A", "a")]
    [InlineData("", "")]
    [InlineData("already", "already")]
    public void ToCamelCase_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToCamelCase();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("userProfile", "UserProfile")]
    [InlineData("calendar", "Calendar")]
    [InlineData("a", "A")]
    [InlineData("", "")]
    [InlineData("Already", "Already")]
    public void ToPascalCase_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToPascalCase();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("UserProfile", "user-profile")]
    [InlineData("Calendar", "calendar")]
    [InlineData("HTTPClient", "h-t-t-p-client")]
    [InlineData("ID", "i-d")]
    [InlineData("", "")]
    public void ToKebabCase_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToKebabCase();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Calendar", "calendar.ts")]
    [InlineData("UserProfile", "user-profile.ts")]
    [InlineData("LeagueKey", "league-key.ts")]
    public void ToTypeScriptFileName_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToTypeScriptFileName();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Calendar", "calendar.service.ts")]
    [InlineData("UserProfile", "user-profile.service.ts")]
    public void ToServiceFileName_ConvertsCorrectly(string input, string expected)
    {
        // Act
        var result = input.ToServiceFileName();

        // Assert
        Assert.Equal(expected, result);
    }
}
