using System.Linq;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.UnitTest.TypescriptGenerator.Models;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for ModelAnalyzer - analyzing complete models.
/// </summary>
public class ModelAnalyzerTest
{
    private readonly ModelAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_Calendar_ReturnsModelDescriptor()
    {
        // Act
        var descriptor = _analyzer.Analyze(typeof(Calendar));

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal("Calendar", descriptor.Name);
        Assert.False(descriptor.IsEnum);
        Assert.Equal(4, descriptor.Properties.Count);
    }

    [Fact]
    public void Analyze_Calendar_RequiresRawType()
    {
        // Act
        var descriptor = _analyzer.Analyze(typeof(Calendar));

        // Assert
        Assert.True(descriptor.RequiresRawType);
        // Calendar has properties with JsonPropertyName attributes
    }

    [Fact]
    public void Analyze_Enum_ReturnsEnumDescriptor()
    {
        // Act
        var descriptor = _analyzer.Analyze(typeof(PlayerRole));

        // Assert
        Assert.True(descriptor.IsEnum);
        Assert.NotNull(descriptor.EnumValues);
        Assert.Equal(4, descriptor.EnumValues!.Count);

        var goalkeeper = descriptor.EnumValues.First(v => v.Name == "Goalkeeper");
        Assert.Equal(1, goalkeeper.Value);

        var forward = descriptor.EnumValues.First(v => v.Name == "Forward");
        Assert.Equal(4, forward.Value);
    }

    [Fact]
    public void Analyze_Model_DiscoversNestedTypes()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        var descriptor = _analyzer.Analyze(typeof(Calendar));

        // Assert
        var analyzedModels = _analyzer.GetAnalyzedModels();

        // Should have analyzed Calendar and its nested types
        Assert.True(analyzedModels.Count > 1);
        Assert.Contains(analyzedModels.Keys, t => t == typeof(Calendar));
    }

    [Fact]
    public void Analyze_Team_DiscoversEnums()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        _analyzer.Analyze(typeof(Team));

        // Assert
        var analyzedModels = _analyzer.GetAnalyzedModels();

        // Should have discovered PlayerRole and TeamStatus enums
        Assert.Contains(analyzedModels.Keys, t => t.Name == "PlayerRole");
        Assert.Contains(analyzedModels.Keys, t => t.Name == "TeamStatus");
    }

    [Fact]
    public void Analyze_SharedType_TracksOwnership()
    {
        // Arrange
        _analyzer.Clear();

        // Act - Analyze Calendar first (owns LeagueKey at depth 0)
        _analyzer.Analyze(typeof(Calendar));
        // Then analyze Team (uses LeagueKey at depth 1)
        _analyzer.Analyze(typeof(Team));
        // Then analyze Rank (uses LeagueKey at depth 1)
        _analyzer.Analyze(typeof(Rank));

        // Assert
        var ownership = _analyzer.GetTypeOwnership();

        // LeagueKey should be owned by the first model that discovered it
        // In this case, Team discovers it at depth 1 (as a property)
        Assert.Contains(ownership.Keys, k => k == "LeagueKey");
    }

    [Fact]
    public void Analyze_CachesResults()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        var descriptor1 = _analyzer.Analyze(typeof(Calendar));
        var descriptor2 = _analyzer.Analyze(typeof(Calendar));

        // Assert - should be same instance due to caching
        Assert.Same(descriptor1, descriptor2);
    }

    [Fact]
    public void Analyze_CalendarDay_ExtractsNestedArrayType()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        var descriptor = _analyzer.Analyze(typeof(CalendarDay));

        // Assert
        var gamesProperty = descriptor.Properties.First(p => p.CSharpName == "Games");
        Assert.True(gamesProperty.Type.IsArray);
        Assert.Equal("CalendarGame", gamesProperty.Type.ElementType!.TypeScriptName);
    }

    [Fact]
    public void FindByName_ExistingModel_ReturnsDescriptor()
    {
        // Arrange
        _analyzer.Clear();
        _analyzer.Analyze(typeof(Calendar));

        // Act
        var descriptor = _analyzer.FindByName("Calendar");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal("Calendar", descriptor.Name);
    }

    [Fact]
    public void FindByName_NonExistingModel_ReturnsNull()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        var descriptor = _analyzer.FindByName("NonExistentModel");

        // Assert
        Assert.Null(descriptor);
    }

    [Fact]
    public void Analyze_LeagueKey_CorrectProperties()
    {
        // Act
        var descriptor = _analyzer.Analyze(typeof(LeagueKey));

        // Assert
        Assert.Equal(3, descriptor.Properties.Count);
        Assert.True(descriptor.RequiresRawType); // Has JsonPropertyName attributes
    }

    [Fact]
    public void GetFileName_ReturnsLowercaseTsFile()
    {
        // Act
        var descriptor = _analyzer.Analyze(typeof(Calendar));

        // Assert
        Assert.Equal("calendar.ts", descriptor.GetFileName());
    }

    [Fact]
    public void GetRawTypeName_AppendsRaw()
    {
        // Act
        var descriptor = _analyzer.Analyze(typeof(Calendar));

        // Assert
        Assert.Equal("CalendarRaw", descriptor.GetRawTypeName());
    }

    [Fact]
    public void GetCleanTypeName_ReturnsOriginalName()
    {
        // Act
        var descriptor = _analyzer.Analyze(typeof(Calendar));

        // Assert
        Assert.Equal("Calendar", descriptor.GetCleanTypeName());
    }
}
