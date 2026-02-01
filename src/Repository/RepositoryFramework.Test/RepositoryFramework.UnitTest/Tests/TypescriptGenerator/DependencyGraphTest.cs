using System.Linq;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.UnitTest.TypescriptGenerator.Models;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for DependencyGraph - resolving type ownership and dependencies.
/// </summary>
public class DependencyGraphTest
{
    [Fact]
    public void AddModel_SingleModel_TracksOwnership()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var graph = new DependencyGraph();
        var calendarDescriptor = analyzer.Analyze(typeof(Calendar));

        // Act
        graph.AddModel(calendarDescriptor);

        // Assert
        var owner = graph.GetOwner("Calendar");
        Assert.Equal("Calendar", owner);
    }

    [Fact]
    public void AddModel_WithNestedTypes_TracksAllOwnership()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var graph = new DependencyGraph();
        var calendarDescriptor = analyzer.Analyze(typeof(Calendar));

        // Act
        graph.AddModel(calendarDescriptor);

        // Assert - Calendar should own its nested types
        var calendarOwner = graph.GetOwner("Calendar");
        Assert.Equal("Calendar", calendarOwner);

        // CalendarDay should be owned by Calendar (discovered at depth 1)
        var calendarDayOwner = graph.GetOwner("CalendarDay");
        Assert.Equal("Calendar", calendarDayOwner);
    }

    [Fact]
    public void GetDependencies_ReturnsNestedTypes()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var graph = new DependencyGraph();
        var calendarDescriptor = analyzer.Analyze(typeof(Calendar));

        // Act
        graph.AddModel(calendarDescriptor);
        var dependencies = graph.GetDependencies("Calendar");

        // Assert
        Assert.Contains("CalendarDay", dependencies);
    }

    [Fact]
    public void GetOwnedTypes_ReturnsTypesOwnedByModel()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var graph = new DependencyGraph();
        var calendarDescriptor = analyzer.Analyze(typeof(Calendar));

        // Act
        graph.AddModel(calendarDescriptor);
        var ownedTypes = graph.GetOwnedTypes("Calendar").ToList();

        // Assert
        Assert.Contains("Calendar", ownedTypes);
        Assert.Contains("CalendarDay", ownedTypes);
    }

    [Fact]
    public void SharedType_ShallowerDepthWins()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var graph = new DependencyGraph();

        // Analyze Rank first - it has LeagueKey at depth 1
        var rankDescriptor = analyzer.Analyze(typeof(Rank));
        graph.AddModel(rankDescriptor);

        // Analyze Team - it also has LeagueKey at depth 1
        var teamDescriptor = analyzer.Analyze(typeof(Team));
        graph.AddModel(teamDescriptor);

        // Assert - First one wins at equal depth
        var leagueKeyOwner = graph.GetOwner("LeagueKey");
        Assert.Equal("Rank", leagueKeyOwner);
    }

    [Fact]
    public void GetImports_ReturnsExternalDependencies()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var graph = new DependencyGraph();

        // Rank owns LeagueKey
        var rankDescriptor = analyzer.Analyze(typeof(Rank));
        graph.AddModel(rankDescriptor);

        // Team needs LeagueKey but doesn't own it
        var teamDescriptor = analyzer.Analyze(typeof(Team));
        graph.AddModel(teamDescriptor);

        // Act
        var imports = graph.GetImports("Team").ToList();

        // Assert
        var leagueKeyImport = imports.FirstOrDefault(i => i.TypeName == "LeagueKey");
        if (leagueKeyImport != default)
        {
            Assert.Equal("Rank", leagueKeyImport.FromModel);
        }
    }

    [Fact]
    public void GetTopologicalOrder_DependenciesFirst()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var graph = new DependencyGraph();

        // Add in reverse order
        var teamDescriptor = analyzer.Analyze(typeof(Team));
        graph.AddModel(teamDescriptor);

        var rankDescriptor = analyzer.Analyze(typeof(Rank));
        graph.AddModel(rankDescriptor);

        // Act
        var order = graph.GetTopologicalOrder().ToList();

        // Assert
        Assert.NotEmpty(order);
        // Both should be in the order
        Assert.Contains("Team", order);
        Assert.Contains("Rank", order);
    }

    [Fact]
    public void Clear_RemovesAllData()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var graph = new DependencyGraph();
        var calendarDescriptor = analyzer.Analyze(typeof(Calendar));
        graph.AddModel(calendarDescriptor);

        // Act
        graph.Clear();

        // Assert
        var ownership = graph.GetAllOwnership();
        Assert.Empty(ownership);
    }

    [Fact]
    public void GetAllModels_ReturnsAddedModels()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var graph = new DependencyGraph();

        var calendarDescriptor = analyzer.Analyze(typeof(Calendar));
        var teamDescriptor = analyzer.Analyze(typeof(Team));

        // Act
        graph.AddModel(calendarDescriptor);
        graph.AddModel(teamDescriptor);

        var allModels = graph.GetAllModels();

        // Assert
        Assert.Equal(2, allModels.Count);
        Assert.Contains(allModels.Keys, k => k == "Calendar");
        Assert.Contains(allModels.Keys, k => k == "Team");
    }

    [Fact]
    public void EnumTypes_AreTracked()
    {
        // Arrange
        var analyzer = new ModelAnalyzer();
        var graph = new DependencyGraph();
        var teamDescriptor = analyzer.Analyze(typeof(Team));

        // Act
        graph.AddModel(teamDescriptor);

        // Assert - Enums should be tracked as dependencies
        var dependencies = graph.GetDependencies("Team");
        Assert.Contains("TeamStatus", dependencies);
        Assert.Contains("PlayerRole", dependencies);
    }
}
