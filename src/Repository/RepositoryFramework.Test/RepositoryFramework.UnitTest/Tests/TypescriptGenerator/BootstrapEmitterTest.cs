using System.Collections.Generic;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.Services;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for BootstrapEmitter - generates the repositorySetup.ts file with transformers.
/// </summary>
public class BootstrapEmitterTest
{
    [Fact]
    public void Emit_ImportsRepositoryServices()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "LeagueKey", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor> { { "LeagueKey", TestDescriptorFactory.CreateModel("LeagueKey") } };

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("import { RepositoryServices } from 'rystem.repository.client';", result);
        Assert.Contains("import type { RepositoryEndpoint } from 'rystem.repository.client';", result);
    }

    [Fact]
    public void Emit_ImportsTransformers()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "LeagueKey", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor> { { "LeagueKey", TestDescriptorFactory.CreateModel("LeagueKey") } };

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("CalendarTransformer", result);
        Assert.Contains("LeagueKeyTransformer", result);
        Assert.Contains("from '../transformers/CalendarTransformer'", result);
        Assert.Contains("from '../transformers/LeagueKeyTransformer'", result);
    }

    [Fact]
    public void Emit_ImportsCleanTypesWithImportType()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "LeagueKey", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor> { { "LeagueKey", TestDescriptorFactory.CreateModel("LeagueKey") } };

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("import type { Calendar } from '../types/calendar';", result);
        Assert.Contains("import type { LeagueKey } from '../types/leaguekey';", result);
    }

    [Fact]
    public void Emit_GeneratesConfigInterface()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "string", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("export interface RepositoryConfig", result);
        Assert.Contains("baseUrl: string;", result);
        Assert.Contains("headersEnricher?:", result);
        Assert.Contains("errorHandler?:", result);
    }

    [Fact]
    public void Emit_GeneratesSetupFunction()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "string", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("export const setupRepositoryServices = (config: RepositoryConfig): void =>", result);
        Assert.Contains("RepositoryServices.Create(config.baseUrl)", result);
    }

    [Fact]
    public void Emit_RegistersRepositoryWithCleanTypesAndTransformers()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "LeagueKey", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor> { { "LeagueKey", TestDescriptorFactory.CreateModel("LeagueKey") } };

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("services.addRepository<Calendar, LeagueKey>", result);
        Assert.Contains("x.name = 'calendar'", result);
        // Without BackendFactoryName, path is just the ModelName
        Assert.Contains("x.path = 'Calendar'", result);
        Assert.Contains("x.transformer = CalendarTransformer;", result);
        Assert.Contains("x.keyTransformer = LeagueKeyTransformer;", result);
    }

    [Fact]
    public void Emit_WithBackendFactoryName_PathIsModelSlashBackendFactory()
    {
        // Arrange - BackendFactoryName is the same as FactoryName
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Rank", KeyName = "RankKey", Kind = RepositoryKind.Repository, FactoryName = "rank", BackendFactoryName = "rank" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Rank", TestDescriptorFactory.CreateModel("Rank") } };
        var keys = new Dictionary<string, ModelDescriptor> { { "RankKey", TestDescriptorFactory.CreateModel("RankKey") } };

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("x.name = 'rank'", result);
        Assert.Contains("x.path = 'Rank/rank'", result);
    }

    [Fact]
    public void Emit_WithDifferentBackendFactoryName_PathUsesDifferentName()
    {
        // Arrange - BackendFactoryName is different from FactoryName
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Rank", KeyName = "RankKey", Kind = RepositoryKind.Repository, FactoryName = "rank", BackendFactoryName = "rankApi" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Rank", TestDescriptorFactory.CreateModel("Rank") } };
        var keys = new Dictionary<string, ModelDescriptor> { { "RankKey", TestDescriptorFactory.CreateModel("RankKey") } };

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("x.name = 'rank'", result);
        Assert.Contains("x.path = 'Rank/rankApi'", result);
    }

    [Fact]
    public void Emit_WithoutBackendFactoryName_PathIsJustModelName()
    {
        // Arrange - No BackendFactoryName (null)
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Rank", KeyName = "RankKey", Kind = RepositoryKind.Repository, FactoryName = "rank", BackendFactoryName = null }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Rank", TestDescriptorFactory.CreateModel("Rank") } };
        var keys = new Dictionary<string, ModelDescriptor> { { "RankKey", TestDescriptorFactory.CreateModel("RankKey") } };

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("x.name = 'rank'", result);
        Assert.Contains("x.path = 'Rank'", result);
    }

    [Fact]
    public void Emit_RegistersQuery()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Team", KeyName = "Guid", Kind = RepositoryKind.Query, FactoryName = "teams" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Team", TestDescriptorFactory.CreateModel("Team") } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("services.addQuery<Team, string>", result);
        Assert.Contains("x.name = 'teams'", result);
        Assert.Contains("x.transformer = TeamTransformer;", result);
    }

    [Fact]
    public void Emit_RegistersCommand()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Order", KeyName = "int", Kind = RepositoryKind.Command, FactoryName = "orders" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Order", TestDescriptorFactory.CreateModel("Order") } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("services.addCommand<Order, number>", result);
        Assert.Contains("x.transformer = OrderTransformer;", result);
    }

    [Fact]
    public void Emit_SetsComplexKeyForNonPrimitiveKeys()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "LeagueKey", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor> { { "LeagueKey", TestDescriptorFactory.CreateModel("LeagueKey") } };

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("x.complexKey = true;", result);
    }

    [Fact]
    public void Emit_DoesNotSetComplexKeyForPrimitiveKeys()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "User", KeyName = "string", Kind = RepositoryKind.Repository, FactoryName = "users" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "User", TestDescriptorFactory.CreateModel("User") } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.DoesNotContain("x.complexKey = true;", result);
        Assert.DoesNotContain("x.keyTransformer", result);
    }

    [Fact]
    public void Emit_AddsHeadersEnricherConditionally()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "User", KeyName = "Guid", Kind = RepositoryKind.Repository, FactoryName = "users" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "User", TestDescriptorFactory.CreateModel("User") } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("if (config.headersEnricher) x.addHeadersEnricher(config.headersEnricher);", result);
    }

    [Fact]
    public void Emit_AddsErrorHandlerConditionally()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "User", KeyName = "Guid", Kind = RepositoryKind.Repository, FactoryName = "users" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "User", TestDescriptorFactory.CreateModel("User") } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("if (config.errorHandler) x.addErrorHandler(config.errorHandler);", result);
    }

    [Fact]
    public void Emit_MultipleRepositories_RegistersAll()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "LeagueKey", Kind = RepositoryKind.Repository, FactoryName = "calendar" },
            new() { ModelName = "Team", KeyName = "Guid", Kind = RepositoryKind.Query, FactoryName = "teams" },
            new() { ModelName = "Match", KeyName = "int", Kind = RepositoryKind.Command, FactoryName = "matches" }
        };
        var models = new Dictionary<string, ModelDescriptor>
        {
            { "Calendar", TestDescriptorFactory.CreateModel("Calendar") },
            { "Team", TestDescriptorFactory.CreateModel("Team") },
            { "Match", TestDescriptorFactory.CreateModel("Match") }
        };
        var keys = new Dictionary<string, ModelDescriptor> { { "LeagueKey", TestDescriptorFactory.CreateModel("LeagueKey") } };

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("addRepository<Calendar, LeagueKey>", result);
        Assert.Contains("addQuery<Team, string>", result);
        Assert.Contains("addCommand<Match, number>", result);
        Assert.Contains("CalendarTransformer", result);
        Assert.Contains("TeamTransformer", result);
        Assert.Contains("MatchTransformer", result);
    }

    [Fact]
    public void Emit_FullyQualifiedNames_ExtractsSimpleName()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Fantacalcio.Domain.Calendar", KeyName = "Fantacalcio.Domain.LeagueKey", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor> { { "LeagueKey", TestDescriptorFactory.CreateModel("LeagueKey") } };

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("CalendarTransformer", result);
        Assert.Contains("LeagueKeyTransformer", result);
        Assert.Contains("from '../transformers/CalendarTransformer'", result);
    }

    [Fact]
    public void Emit_IncludesUsageExample()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "User", KeyName = "string", Kind = RepositoryKind.Repository, FactoryName = "users" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "User", TestDescriptorFactory.CreateModel("User") } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("@example", result);
        Assert.Contains("setupRepositoryServices({", result);
        Assert.Contains("baseUrl:", result);
    }

    [Fact]
    public void Emit_GenericModelWithNamespacedArgs_ExtractsSimpleNames()
    {
        // Arrange - simulates the bug where GetSimpleName returned "Paragraph>" instead of "EntityVersions<Paragraph>"
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Namespace.EntityVersions<Namespace.Paragraph>", KeyName = "string", Kind = RepositoryKind.Repository, FactoryName = "versionedparagraphs" }
        };
        var openGenericModel = new ModelDescriptor
        {
            Name = "EntityVersions",
            FullName = "Test.EntityVersions",
            Namespace = "Test",
            Properties = [],
            IsEnum = false,
            GenericTypeParameters = ["T"]
        };
        var models = new Dictionary<string, ModelDescriptor> { { "EntityVersions", openGenericModel } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert - should use full generic name, not just "Paragraph>"
        Assert.Contains("// EntityVersions<Paragraph> (Repository)", result);
        Assert.Contains("services.addRepository<EntityVersions<Paragraph>, string>", result);
        Assert.DoesNotContain("Paragraph>", result.Replace("EntityVersions<Paragraph>", ""));
    }

    [Fact]
    public void Emit_GenericModel_PathUsesClrStyleFormat()
    {
        // Arrange - verifies the path matches the server CLR-style format
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "EntityVersions<Paragraph>", KeyName = "string", Kind = RepositoryKind.Repository, FactoryName = "versionedparagraphs" }
        };
        var openGenericModel = new ModelDescriptor
        {
            Name = "EntityVersions",
            FullName = "Test.EntityVersions",
            Namespace = "Test",
            Properties = [],
            IsEnum = false,
            GenericTypeParameters = ["T"]
        };
        var models = new Dictionary<string, ModelDescriptor> { { "EntityVersions", openGenericModel } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = BootstrapEmitter.Emit(repos, models, keys);

        // Assert - path should use CLR backtick format matching the server
        Assert.Contains("x.path = 'EntityVersions`1Paragraph'", result);
    }
}
