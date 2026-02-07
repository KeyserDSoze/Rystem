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

    [Fact]
    public void Analyze_DictionaryStringString_DoesNotGenerateModel()
    {
        // Arrange
        _analyzer.Clear();

        // Act - Inventory has Dictionary<string, string> Tags
        _analyzer.Analyze(typeof(Inventory));

        // Assert - Dictionary<string, string> should NOT be in the analyzed models
        var analyzedModels = _analyzer.GetAnalyzedModels();
        Assert.DoesNotContain(analyzedModels.Values, m =>
            m.Name.Contains("Dictionary") || m.Name.Contains("IDictionary"));
    }

    [Fact]
    public void Analyze_DictionaryWithComplexValue_DiscoversValueType()
    {
        // Arrange
        _analyzer.Clear();

        // Act - Inventory has Dictionary<string, InventoryItem> Items
        _analyzer.Analyze(typeof(Inventory));

        // Assert - InventoryItem should be analyzed, Dictionary should not
        var analyzedModels = _analyzer.GetAnalyzedModels();
        Assert.Contains(analyzedModels.Values, m => m.Name == "InventoryItem");
        Assert.DoesNotContain(analyzedModels.Values, m => m.Name.Contains("Dictionary"));
    }

    [Fact]
    public void Analyze_ListOfString_DoesNotGenerateModel()
    {
        // Arrange
        _analyzer.Clear();

        // Act - Inventory has List<string> Labels
        _analyzer.Analyze(typeof(Inventory));

        // Assert - List<> should NOT be in the analyzed models
        var analyzedModels = _analyzer.GetAnalyzedModels();
        Assert.DoesNotContain(analyzedModels.Values, m =>
            m.Name.Contains("List") || m.Name.Contains("IList"));
    }

    [Fact]
    public void Analyze_IDictionaryAndIReadOnlyDictionary_DoNotGenerateModels()
    {
        // Arrange
        _analyzer.Clear();

        // Act - Inventory has IDictionary<string, bool> and IReadOnlyDictionary<string, InventoryItem>
        _analyzer.Analyze(typeof(Inventory));

        // Assert
        var analyzedModels = _analyzer.GetAnalyzedModels();
        Assert.DoesNotContain(analyzedModels.Values, m =>
            m.Name.Contains("Dictionary") || m.Name.Contains("ReadOnlyDictionary"));
    }

    [Fact]
    public void Analyze_DictionaryProperty_TypeScriptNameIsRecord()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        var descriptor = _analyzer.Analyze(typeof(Inventory));

        // Assert - Tags property should be Record<string, string>
        var tagsProperty = descriptor.Properties.First(p => p.CSharpName == "Tags");
        Assert.True(tagsProperty.Type.IsDictionary);
        Assert.Equal("Record<string, string>", tagsProperty.Type.TypeScriptName);

        // Quantities should be Record<string, number>
        var quantitiesProperty = descriptor.Properties.First(p => p.CSharpName == "Quantities");
        Assert.True(quantitiesProperty.Type.IsDictionary);
        Assert.Equal("Record<string, number>", quantitiesProperty.Type.TypeScriptName);
    }

    [Fact]
    public void Analyze_CalendarRounds_DictionaryNotAnalyzedAsModel()
    {
        // Arrange - Calendar has Dictionary<string, CalendarDay[]> Rounds
        _analyzer.Clear();

        // Act
        _analyzer.Analyze(typeof(Calendar));

        // Assert - Dictionary should not be a model, but CalendarDay should be
        var analyzedModels = _analyzer.GetAnalyzedModels();
        Assert.DoesNotContain(analyzedModels.Values, m => m.Name.Contains("Dictionary"));
        Assert.Contains(analyzedModels.Values, m => m.Name == "CalendarDay");
    }

    [Fact]
    public void Analyze_IEnumerable_DoesNotGenerateModel()
    {
        // Arrange
        _analyzer.Clear();

        // Act - Inventory has IEnumerable<string> Emails
        _analyzer.Analyze(typeof(Inventory));

        // Assert
        var analyzedModels = _analyzer.GetAnalyzedModels();
        Assert.DoesNotContain(analyzedModels.Values, m =>
            m.Name.Contains("IEnumerable") || m.Name.Contains("Enumerable"));
    }

    [Fact]
    public void Analyze_IListAndHashSet_DoNotGenerateModels()
    {
        // Arrange
        _analyzer.Clear();

        // Act - Inventory has IList<int> Scores and HashSet<string> UniqueNames
        _analyzer.Analyze(typeof(Inventory));

        // Assert
        var analyzedModels = _analyzer.GetAnalyzedModels();
        Assert.DoesNotContain(analyzedModels.Values, m =>
            m.Name.Contains("IList") || m.Name.Contains("HashSet") || m.Name.Contains("ISet"));
    }

    [Fact]
    public void Analyze_IReadOnlyListWithComplexElement_DiscoversElementType()
    {
        // Arrange
        _analyzer.Clear();

        // Act - Inventory has IReadOnlyList<InventoryItem> ArchivedItems
        _analyzer.Analyze(typeof(Inventory));

        // Assert - InventoryItem should be discovered, IReadOnlyList should not
        var analyzedModels = _analyzer.GetAnalyzedModels();
        Assert.Contains(analyzedModels.Values, m => m.Name == "InventoryItem");
        Assert.DoesNotContain(analyzedModels.Values, m =>
            m.Name.Contains("ReadOnlyList") || m.Name.Contains("IReadOnlyList"));
    }

    [Fact]
    public void Analyze_Inventory_NoCollectionTypesInAnalyzedModels()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        _analyzer.Analyze(typeof(Inventory));

        // Assert - ONLY Inventory and InventoryItem should be analyzed, nothing else
        var analyzedModels = _analyzer.GetAnalyzedModels();
        var modelNames = analyzedModels.Values.Select(m => m.Name).ToList();

        Assert.Contains("Inventory", modelNames);
        Assert.Contains("InventoryItem", modelNames);
        Assert.Equal(2, modelNames.Count); // Only these two, no collection types
    }

    [Fact]
    public void Analyze_CollectionProperties_TypeScriptNamesAreArrays()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        var descriptor = _analyzer.Analyze(typeof(Inventory));

        // IEnumerable<string> → string[]
        var emailsProp = descriptor.Properties.First(p => p.CSharpName == "Emails");
        Assert.True(emailsProp.Type.IsArray);
        Assert.Equal("string[]", emailsProp.Type.TypeScriptName);

        // IList<int> → number[]
        var scoresProp = descriptor.Properties.First(p => p.CSharpName == "Scores");
        Assert.True(scoresProp.Type.IsArray);
        Assert.Equal("number[]", scoresProp.Type.TypeScriptName);

        // HashSet<string> → string[]
        var uniqueNamesProp = descriptor.Properties.First(p => p.CSharpName == "UniqueNames");
        Assert.True(uniqueNamesProp.Type.IsArray);
        Assert.Equal("string[]", uniqueNamesProp.Type.TypeScriptName);

        // IDictionary<string, bool> → Record<string, boolean>
        var flagsProp = descriptor.Properties.First(p => p.CSharpName == "Flags");
        Assert.True(flagsProp.Type.IsDictionary);
        Assert.Equal("Record<string, boolean>", flagsProp.Type.TypeScriptName);
    }

    [Fact]
    public void Analyze_ChapterLocalization_LocalizedFormatStringNotAnalyzedAsModel()
    {
        // Arrange - ChapterLocalization.Description is LocalizedFormatString
        // which has [JsonConverter] → should be treated as string, NOT as a model
        _analyzer.Clear();

        // Act
        _analyzer.Analyze(typeof(ChapterLocalization));

        // Assert
        var analyzedModels = _analyzer.GetAnalyzedModels();

        // LocalizedFormatString should NOT be in analyzed models (it's primitive via JsonConverter)
        Assert.DoesNotContain(analyzedModels.Values, m =>
            m.Name == "LocalizedFormatString");

        // Only ChapterLocalization should be a model
        Assert.Contains(analyzedModels.Values, m => m.Name == "ChapterLocalization");
    }

    [Fact]
    public void Analyze_ChapterLocalization_DescriptionPropertyIsString()
    {
        // Arrange
        _analyzer.Clear();

        // Act
        var descriptor = _analyzer.Analyze(typeof(ChapterLocalization));

        // Assert - Description should be "string" not "LocalizedFormatString"
        var descProp = descriptor.Properties.First(p => p.CSharpName == "Description");
        Assert.Equal("string", descProp.Type.TypeScriptName);
        Assert.True(descProp.Type.IsPrimitive);
    }
}
