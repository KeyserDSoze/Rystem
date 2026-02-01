using System.Collections.Generic;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using Xunit;
using static RepositoryFramework.UnitTest.TypescriptGenerator.TestDescriptorFactory;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for EmitterContext - shared context for TypeScript emission.
/// </summary>
public class EmitterContextTest
{
    [Fact]
    public void FromAnalysisResult_PopulatesTypesRequiringRaw()
    {
        // Arrange
        var modelWithCustomJson = CreateModel("User",
            CreateProperty("Name", "n", StringType)); // Has custom JSON name
        var modelWithoutCustomJson = CreateModel("Simple",
            CreateProperty("Name", StringType)); // No custom JSON name

        var models = new List<ModelDescriptor> { modelWithCustomJson, modelWithoutCustomJson };
        var keys = new List<ModelDescriptor>();
        var ownership = new Dictionary<string, string>();

        // Act
        var context = EmitterContext.FromAnalysisResult(models, keys, ownership);

        // Assert
        Assert.Contains("User", context.TypesRequiringRaw);
        Assert.DoesNotContain("Simple", context.TypesRequiringRaw);
    }

    [Fact]
    public void FromAnalysisResult_PopulatesEnumTypes()
    {
        // Arrange
        var enumModel = CreateEnum("Status", ("Active", 0), ("Inactive", 1));
        var classModel = CreateModel("User", CreateProperty("Id", NumberType));

        var models = new List<ModelDescriptor> { enumModel, classModel };
        var keys = new List<ModelDescriptor>();
        var ownership = new Dictionary<string, string>();

        // Act
        var context = EmitterContext.FromAnalysisResult(models, keys, ownership);

        // Assert
        Assert.Contains("Status", context.EnumTypes);
        Assert.DoesNotContain("User", context.EnumTypes);
    }

    [Fact]
    public void FromAnalysisResult_PopulatesAllModels()
    {
        // Arrange
        var model1 = CreateModel("User", CreateProperty("Id", NumberType));
        var model2 = CreateModel("Order", CreateProperty("Id", NumberType));
        var key = CreateModel("UserId", CreateProperty("Value", StringType));

        var models = new List<ModelDescriptor> { model1, model2 };
        var keys = new List<ModelDescriptor> { key };
        var ownership = new Dictionary<string, string>();

        // Act
        var context = EmitterContext.FromAnalysisResult(models, keys, ownership);

        // Assert
        Assert.Equal(3, context.AllModels.Count);
        Assert.True(context.AllModels.ContainsKey("User"));
        Assert.True(context.AllModels.ContainsKey("Order"));
        Assert.True(context.AllModels.ContainsKey("UserId"));
    }

    [Fact]
    public void FromAnalysisResult_CopiesTypeOwnership()
    {
        // Arrange
        var model = CreateModel("Calendar", CreateProperty("Year", NumberType));
        var models = new List<ModelDescriptor> { model };
        var keys = new List<ModelDescriptor>();
        var ownership = new Dictionary<string, string>
        {
            { "CalendarDay", "Calendar" },
            { "CalendarGame", "Calendar" }
        };

        // Act
        var context = EmitterContext.FromAnalysisResult(models, keys, ownership);

        // Assert
        Assert.Equal("Calendar", context.TypeOwnership["CalendarDay"]);
        Assert.Equal("Calendar", context.TypeOwnership["CalendarGame"]);
    }

    [Fact]
    public void FromAnalysisResult_ProcessesNestedTypes()
    {
        // Arrange
        var nestedType = CreateModel("Address", CreateProperty("Street", "s", StringType));
        var parentModel = new ModelDescriptor
        {
            Name = "User",
            FullName = "Test.User",
            Namespace = "Test",
            IsEnum = false,
            Properties = [CreateProperty("Name", "n", StringType)],
            NestedTypes = [nestedType]
        };

        var models = new List<ModelDescriptor> { parentModel };
        var keys = new List<ModelDescriptor>();
        var ownership = new Dictionary<string, string>();

        // Act
        var context = EmitterContext.FromAnalysisResult(models, keys, ownership);

        // Assert
        Assert.Contains("User", context.TypesRequiringRaw);
        Assert.Contains("Address", context.TypesRequiringRaw);
        Assert.True(context.AllModels.ContainsKey("Address"));
    }

    [Fact]
    public void FromAnalysisResult_ProcessesNestedEnums()
    {
        // Arrange
        var nestedEnum = CreateEnum("UserStatus", ("Active", 0));
        var parentModel = new ModelDescriptor
        {
            Name = "User",
            FullName = "Test.User",
            Namespace = "Test",
            IsEnum = false,
            Properties = [],
            NestedTypes = [nestedEnum]
        };

        var models = new List<ModelDescriptor> { parentModel };
        var keys = new List<ModelDescriptor>();
        var ownership = new Dictionary<string, string>();

        // Act
        var context = EmitterContext.FromAnalysisResult(models, keys, ownership);

        // Assert
        Assert.Contains("UserStatus", context.EnumTypes);
    }

    [Fact]
    public void FromAnalysisResult_HandlesEmptyInputs()
    {
        // Arrange
        var models = new List<ModelDescriptor>();
        var keys = new List<ModelDescriptor>();
        var ownership = new Dictionary<string, string>();

        // Act
        var context = EmitterContext.FromAnalysisResult(models, keys, ownership);

        // Assert
        Assert.Empty(context.AllModels);
        Assert.Empty(context.TypesRequiringRaw);
        Assert.Empty(context.EnumTypes);
        Assert.Empty(context.TypeOwnership);
    }

    [Fact]
    public void FromAnalysisResult_KeysWithRawType_AddedToTypesRequiringRaw()
    {
        // Arrange
        var key = CreateModel("LeagueKey", CreateProperty("Id", "i", NumberType)); // Custom JSON name

        var models = new List<ModelDescriptor>();
        var keys = new List<ModelDescriptor> { key };
        var ownership = new Dictionary<string, string>();

        // Act
        var context = EmitterContext.FromAnalysisResult(models, keys, ownership);

        // Assert
        Assert.Contains("LeagueKey", context.TypesRequiringRaw);
    }

    [Fact]
    public void TypesRequiringRaw_IsHashSet_PreventsDuplicates()
    {
        // Arrange
        var context = new EmitterContext();

        // Act
        context.TypesRequiringRaw.Add("User");
        context.TypesRequiringRaw.Add("User");
        context.TypesRequiringRaw.Add("User");

        // Assert
        Assert.Single(context.TypesRequiringRaw);
    }

    [Fact]
    public void EnumTypes_IsHashSet_PreventsDuplicates()
    {
        // Arrange
        var context = new EmitterContext();

        // Act
        context.EnumTypes.Add("Status");
        context.EnumTypes.Add("Status");

        // Assert
        Assert.Single(context.EnumTypes);
    }
}
