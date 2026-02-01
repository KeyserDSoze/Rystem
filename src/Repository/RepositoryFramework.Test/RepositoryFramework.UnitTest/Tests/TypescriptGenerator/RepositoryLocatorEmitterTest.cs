using System.Collections.Generic;
using System.Linq;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.Services;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for RepositoryLocatorEmitter - generates the repositoryLocator.ts file.
/// </summary>
public class RepositoryLocatorEmitterTest
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
        var result = RepositoryLocatorEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("import { RepositoryServices } from 'rystem.repository.client';", result);
    }

    [Fact]
    public void Emit_ImportsInterfaces_OnlyUsedOnes()
    {
        // Arrange - only Repository kind, so only IRepository should be imported
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "LeagueKey", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor> { { "LeagueKey", TestDescriptorFactory.CreateModel("LeagueKey") } };

        // Act
        var result = RepositoryLocatorEmitter.Emit(repos, models, keys);

        // Assert - only IRepository is imported
        Assert.Contains("import type { IRepository } from 'rystem.repository.client';", result);
        Assert.DoesNotContain("IQuery", result.Split('\n').First(l => l.Contains("import type")));
        Assert.DoesNotContain("ICommand", result.Split('\n').First(l => l.Contains("import type")));
    }

    [Fact]
    public void Emit_ImportsTypesWithImportType()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "LeagueKey", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor> { { "LeagueKey", TestDescriptorFactory.CreateModel("LeagueKey") } };

        // Act
        var result = RepositoryLocatorEmitter.Emit(repos, models, keys);

        // Assert - paths use ../types/ since file is in services/ folder
        Assert.Contains("import type { Calendar } from '../types/calendar';", result);
        Assert.Contains("import type { LeagueKey } from '../types/leaguekey';", result);
    }

    [Fact]
    public void Emit_GeneratesRepositoryLocatorConst()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "LeagueKey", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor> { { "LeagueKey", TestDescriptorFactory.CreateModel("LeagueKey") } };

        // Act
        var result = RepositoryLocatorEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("export const RepositoryLocator = {", result);
        Assert.Contains("} as const;", result);
    }

    [Fact]
    public void Emit_GeneratesGetterForRepository()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "LeagueKey", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor> { { "LeagueKey", TestDescriptorFactory.CreateModel("LeagueKey") } };

        // Act
        var result = RepositoryLocatorEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("get calendar(): IRepository<Calendar, LeagueKey>", result);
        Assert.Contains("return RepositoryServices.Repository<Calendar, LeagueKey>('calendar');", result);
    }

    [Fact]
    public void Emit_GeneratesGetterForQuery()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Team", KeyName = "Guid", Kind = RepositoryKind.Query, FactoryName = "teams" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Team", TestDescriptorFactory.CreateModel("Team") } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = RepositoryLocatorEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("get teams(): IQuery<Team, string>", result);
        Assert.Contains("return RepositoryServices.Query<Team, string>('teams');", result);
    }

    [Fact]
    public void Emit_GeneratesGetterForCommand()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Order", KeyName = "int", Kind = RepositoryKind.Command, FactoryName = "orders" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Order", TestDescriptorFactory.CreateModel("Order") } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = RepositoryLocatorEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("get orders(): ICommand<Order, number>", result);
        Assert.Contains("return RepositoryServices.Command<Order, number>('orders');", result);
    }

    [Fact]
    public void Emit_MultipleRepositories_GeneratesAllGetters()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "LeagueKey", Kind = RepositoryKind.Repository, FactoryName = "calendar" },
            new() { ModelName = "Team", KeyName = "Guid", Kind = RepositoryKind.Query, FactoryName = "teams" },
            new() { ModelName = "Order", KeyName = "int", Kind = RepositoryKind.Command, FactoryName = "orders" }
        };
        var models = new Dictionary<string, ModelDescriptor>
        {
            { "Calendar", TestDescriptorFactory.CreateModel("Calendar") },
            { "Team", TestDescriptorFactory.CreateModel("Team") },
            { "Order", TestDescriptorFactory.CreateModel("Order") }
        };
        var keys = new Dictionary<string, ModelDescriptor> { { "LeagueKey", TestDescriptorFactory.CreateModel("LeagueKey") } };

        // Act
        var result = RepositoryLocatorEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("get calendar():", result);
        Assert.Contains("get teams():", result);
        Assert.Contains("get orders():", result);
    }

    [Fact]
    public void Emit_PrimitiveKeyTypes_ConvertsToTypeScript()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "User", KeyName = "Guid", Kind = RepositoryKind.Repository, FactoryName = "users" },
            new() { ModelName = "Product", KeyName = "int", Kind = RepositoryKind.Repository, FactoryName = "products" },
            new() { ModelName = "Tag", KeyName = "string", Kind = RepositoryKind.Repository, FactoryName = "tags" }
        };
        var models = new Dictionary<string, ModelDescriptor>
        {
            { "User", TestDescriptorFactory.CreateModel("User") },
            { "Product", TestDescriptorFactory.CreateModel("Product") },
            { "Tag", TestDescriptorFactory.CreateModel("Tag") }
        };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = RepositoryLocatorEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("IRepository<User, string>", result); // Guid -> string
        Assert.Contains("IRepository<Product, number>", result); // int -> number
        Assert.Contains("IRepository<Tag, string>", result); // string -> string
    }

    [Fact]
    public void Emit_IncludesJsDocForLocator()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "string", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = RepositoryLocatorEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("/**", result);
        Assert.Contains("* Provides typed access to all configured repositories.", result);
        Assert.Contains("@example", result);
    }

    [Fact]
    public void Emit_ExportsLocatorType()
    {
        // Arrange
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "string", Kind = RepositoryKind.Repository, FactoryName = "calendar" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Calendar", TestDescriptorFactory.CreateModel("Calendar") } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = RepositoryLocatorEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("export type RepositoryLocatorType = typeof RepositoryLocator;", result);
    }

    [Fact]
    public void GetFileName_ReturnsCorrectName()
    {
        // Act
        var fileName = RepositoryLocatorEmitter.GetFileName();

        // Assert
        Assert.Equal("repositoryLocator.ts", fileName);
    }

    [Fact]
    public void GetFolder_ReturnsServices()
    {
        // Act
        var folder = RepositoryLocatorEmitter.GetFolder();

        // Assert
        Assert.Equal("services", folder);
    }

    [Fact]
    public void Emit_AllKinds_ImportsAllInterfaces()
    {
        // Arrange - all three kinds are used
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Calendar", KeyName = "string", Kind = RepositoryKind.Repository, FactoryName = "calendar" },
            new() { ModelName = "Team", KeyName = "string", Kind = RepositoryKind.Query, FactoryName = "teams" },
            new() { ModelName = "Order", KeyName = "string", Kind = RepositoryKind.Command, FactoryName = "orders" }
        };
        var models = new Dictionary<string, ModelDescriptor>
        {
            { "Calendar", TestDescriptorFactory.CreateModel("Calendar") },
            { "Team", TestDescriptorFactory.CreateModel("Team") },
            { "Order", TestDescriptorFactory.CreateModel("Order") }
        };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = RepositoryLocatorEmitter.Emit(repos, models, keys);

        // Assert - all three interfaces are imported (alphabetically ordered)
        Assert.Contains("import type { ICommand, IQuery, IRepository } from 'rystem.repository.client';", result);
    }

    [Fact]
    public void Emit_OnlyQuery_ImportsOnlyIQuery()
    {
        // Arrange - only Query kind
        var repos = new List<RepositoryDescriptor>
        {
            new() { ModelName = "Team", KeyName = "string", Kind = RepositoryKind.Query, FactoryName = "teams" }
        };
        var models = new Dictionary<string, ModelDescriptor> { { "Team", TestDescriptorFactory.CreateModel("Team") } };
        var keys = new Dictionary<string, ModelDescriptor>();

        // Act
        var result = RepositoryLocatorEmitter.Emit(repos, models, keys);

        // Assert
        Assert.Contains("import type { IQuery } from 'rystem.repository.client';", result);
    }
}
