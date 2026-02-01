using System.Collections.Generic;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.Services;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for BootstrapEmitter - generates the repositorySetup.ts file.
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
        Assert.Contains("import { RepositoryServices, RepositorySettings, RepositoryEndpoint } from 'rystem.repository.client';", result);
    }

    [Fact]
    public void Emit_ImportsRawTypes()
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
        Assert.Contains("CalendarRaw", result);
        Assert.Contains("LeagueKeyRaw", result);
        Assert.Contains("from '../types/Calendar'", result);
        Assert.Contains("from '../types/LeagueKey'", result);
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
    public void Emit_RegistersRepository()
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
        Assert.Contains("services.addRepository<CalendarRaw, LeagueKeyRaw>", result);
        Assert.Contains("x.name = 'calendar'", result);
        Assert.Contains("x.path = 'calendar'", result);
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
        Assert.Contains("services.addQuery<TeamRaw, string>", result);
        Assert.Contains("x.name = 'teams'", result);
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
        Assert.Contains("services.addCommand<OrderRaw, number>", result);
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
        Assert.Contains("addRepository<CalendarRaw, LeagueKeyRaw>", result);
        Assert.Contains("addQuery<TeamRaw, string>", result);
        Assert.Contains("addCommand<MatchRaw, number>", result);
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
        Assert.Contains("CalendarRaw", result);
        Assert.Contains("LeagueKeyRaw", result);
        Assert.Contains("from '../types/Calendar'", result);
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
}
