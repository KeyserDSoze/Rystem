using System.Linq;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.UnitTest.TypescriptGenerator.Models;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for PropertyAnalyzer - extracting property information.
/// </summary>
public class PropertyAnalyzerTest
{
    private readonly PropertyAnalyzer _analyzer;

    public PropertyAnalyzerTest()
    {
        _analyzer = new PropertyAnalyzer(new TypeResolver());
    }

    [Fact]
    public void AnalyzeProperties_Calendar_ExtractsAllProperties()
    {
        // Act
        var properties = _analyzer.AnalyzeProperties(typeof(Calendar));

        // Assert
        Assert.Equal(4, properties.Count);

        var yearProp = properties.First(p => p.CSharpName == "Year");
        var roundsProp = properties.First(p => p.CSharpName == "Rounds");
        var nameProp = properties.First(p => p.CSharpName == "Name");
        var isActiveProp = properties.First(p => p.CSharpName == "IsActive");

        Assert.NotNull(yearProp);
        Assert.NotNull(roundsProp);
        Assert.NotNull(nameProp);
        Assert.NotNull(isActiveProp);
    }

    [Fact]
    public void AnalyzeProperties_WithJsonPropertyName_ExtractsJsonName()
    {
        // Act
        var properties = _analyzer.AnalyzeProperties(typeof(Calendar));

        // Assert
        var yearProp = properties.First(p => p.CSharpName == "Year");
        Assert.Equal("y", yearProp.JsonName);
        Assert.True(yearProp.HasCustomJsonName);

        var roundsProp = properties.First(p => p.CSharpName == "Rounds");
        Assert.Equal("r", roundsProp.JsonName);
        Assert.True(roundsProp.HasCustomJsonName);
    }

    [Fact]
    public void AnalyzeProperties_WithoutJsonPropertyName_UsesCSharpName()
    {
        // Act
        var properties = _analyzer.AnalyzeProperties(typeof(Calendar));

        // Assert
        var isActiveProp = properties.First(p => p.CSharpName == "IsActive");
        Assert.Equal("IsActive", isActiveProp.JsonName);
        Assert.False(isActiveProp.HasCustomJsonName);
    }

    [Fact]
    public void AnalyzeProperties_TypeScriptName_IsCamelCase()
    {
        // Act
        var properties = _analyzer.AnalyzeProperties(typeof(Calendar));

        // Assert
        var yearProp = properties.First(p => p.CSharpName == "Year");
        Assert.Equal("year", yearProp.TypeScriptName);

        var isActiveProp = properties.First(p => p.CSharpName == "IsActive");
        Assert.Equal("isActive", isActiveProp.TypeScriptName);
    }

    [Fact]
    public void AnalyzeProperties_DetectsTypeCorrectly()
    {
        // Act
        var properties = _analyzer.AnalyzeProperties(typeof(Calendar));

        // Assert
        var yearProp = properties.First(p => p.CSharpName == "Year");
        Assert.True(yearProp.Type.IsPrimitive);
        Assert.Equal("number", yearProp.Type.TypeScriptName);

        var roundsProp = properties.First(p => p.CSharpName == "Rounds");
        Assert.True(roundsProp.Type.IsDictionary);
    }

    [Fact]
    public void AnalyzeProperties_Player_DetectsEnumProperty()
    {
        // Act
        var properties = _analyzer.AnalyzeProperties(typeof(Player));

        // Assert
        var roleProp = properties.First(p => p.CSharpName == "Role");
        Assert.True(roleProp.Type.IsEnum);
        Assert.Equal("PlayerRole", roleProp.Type.TypeScriptName);
    }

    [Fact]
    public void AnalyzeProperties_Team_DetectsListProperty()
    {
        // Act
        var properties = _analyzer.AnalyzeProperties(typeof(Team));

        // Assert
        var playersProp = properties.First(p => p.CSharpName == "Players");
        Assert.True(playersProp.Type.IsArray);
        Assert.NotNull(playersProp.Type.ElementType);
        Assert.Equal("Player", playersProp.Type.ElementType!.TypeScriptName);
    }

    [Fact]
    public void AnalyzeProperties_Team_DetectsComplexProperty()
    {
        // Act
        var properties = _analyzer.AnalyzeProperties(typeof(Team));

        // Assert
        var formationProp = properties.First(p => p.CSharpName == "CurrentFormation");
        Assert.False(formationProp.Type.IsPrimitive);
        Assert.False(formationProp.Type.IsArray);
        Assert.Equal("Formation", formationProp.Type.TypeScriptName);
    }

    [Fact]
    public void AnalyzeProperties_NullableProperties_MarkedAsOptional()
    {
        // Act
        var properties = _analyzer.AnalyzeProperties(typeof(Calendar));

        // Assert
        var nameProp = properties.First(p => p.CSharpName == "Name");
        Assert.True(nameProp.IsOptional);
    }

    [Fact]
    public void AnalyzeProperties_ValueTypeProperties_NotOptional()
    {
        // Act
        var properties = _analyzer.AnalyzeProperties(typeof(Calendar));

        // Assert
        var yearProp = properties.First(p => p.CSharpName == "Year");
        Assert.False(yearProp.IsOptional);
        Assert.True(yearProp.IsRequired);

        var isActiveProp = properties.First(p => p.CSharpName == "IsActive");
        Assert.False(isActiveProp.IsOptional);
    }

    [Fact]
    public void AnalyzeProperties_LeagueKey_ExtractsRecordProperties()
    {
        // Act
        var properties = _analyzer.AnalyzeProperties(typeof(LeagueKey));

        // Assert
        Assert.Equal(3, properties.Count);

        var groupProp = properties.First(p => p.CSharpName == "Group");
        Assert.Equal("g", groupProp.JsonName);

        var leagueProp = properties.First(p => p.CSharpName == "League");
        Assert.Equal("l", leagueProp.JsonName);

        var yearProp = properties.First(p => p.CSharpName == "Year");
        Assert.Equal("y", yearProp.JsonName);
    }
}
