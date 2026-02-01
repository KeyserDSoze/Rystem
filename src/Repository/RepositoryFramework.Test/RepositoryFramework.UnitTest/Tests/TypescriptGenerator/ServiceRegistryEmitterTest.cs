using System.Collections.Generic;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.Services;
using Xunit;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for ServiceRegistryEmitter - generating service registry/factory.
/// </summary>
public class ServiceRegistryEmitterTest
{
    private static RepositoryDescriptor CreateRepositoryDescriptor(
        string modelName,
        string keyName,
        RepositoryKind kind = RepositoryKind.Repository,
        string? factoryName = null)
    {
        return new RepositoryDescriptor
        {
            ModelName = modelName,
            KeyName = keyName,
            Kind = kind,
            FactoryName = factoryName ?? modelName
        };
    }

    [Fact]
    public void Emit_SingleRepository_GeneratesImport()
    {
        // Arrange
        var repos = new[]
        {
            CreateRepositoryDescriptor("User", "string")
        };

        // Act
        var result = ServiceRegistryEmitter.Emit(repos);

        // Assert
        Assert.Contains("import { UserService } from './user.service';", result);
    }

    [Fact]
    public void Emit_SingleRepository_ReExportsService()
    {
        // Arrange
        var repos = new[]
        {
            CreateRepositoryDescriptor("User", "string")
        };

        // Act
        var result = ServiceRegistryEmitter.Emit(repos);

        // Assert
        Assert.Contains("export { UserService };", result);
    }

    [Fact]
    public void Emit_MultipleRepositories_GeneratesAllImports()
    {
        // Arrange
        var repos = new[]
        {
            CreateRepositoryDescriptor("User", "string"),
            CreateRepositoryDescriptor("Order", "int"),
            CreateRepositoryDescriptor("Calendar", "LeagueKey", factoryName: "SerieA")
        };

        // Act
        var result = ServiceRegistryEmitter.Emit(repos);

        // Assert
        Assert.Contains("import { UserService }", result);
        Assert.Contains("import { OrderService }", result);
        Assert.Contains("import { SerieAService }", result);
    }

    [Fact]
    public void Emit_GeneratesServiceConfigInterface()
    {
        // Arrange
        var repos = new[]
        {
            CreateRepositoryDescriptor("User", "string")
        };

        // Act
        var result = ServiceRegistryEmitter.Emit(repos);

        // Assert
        Assert.Contains("export interface ServiceConfig", result);
        Assert.Contains("baseUrl: string;", result);
        Assert.Contains("headers?: () => Promise<HeadersInit>;", result);
    }

    [Fact]
    public void Emit_GeneratesServicesClass()
    {
        // Arrange
        var repos = new[]
        {
            CreateRepositoryDescriptor("User", "string")
        };

        // Act
        var result = ServiceRegistryEmitter.Emit(repos);

        // Assert
        Assert.Contains("export class Services", result);
        Assert.Contains("private static config: ServiceConfig | null = null;", result);
        Assert.Contains("private static instances: Map<string, unknown> = new Map();", result);
    }

    [Fact]
    public void Emit_GeneratesConfigureMethod()
    {
        // Arrange
        var repos = new[]
        {
            CreateRepositoryDescriptor("User", "string")
        };

        // Act
        var result = ServiceRegistryEmitter.Emit(repos);

        // Assert
        Assert.Contains("static configure(config: ServiceConfig): void", result);
        Assert.Contains("Services.config = config;", result);
        Assert.Contains("Services.instances.clear();", result);
    }

    [Fact]
    public void Emit_GeneratesEnsureConfiguredMethod()
    {
        // Arrange
        var repos = new[]
        {
            CreateRepositoryDescriptor("User", "string")
        };

        // Act
        var result = ServiceRegistryEmitter.Emit(repos);

        // Assert
        Assert.Contains("private static ensureConfigured(): ServiceConfig", result);
        Assert.Contains("if (!Services.config)", result);
        Assert.Contains("throw new Error('Services not configured", result);
    }

    [Fact]
    public void Emit_GeneratesServiceGetter()
    {
        // Arrange
        var repos = new[]
        {
            CreateRepositoryDescriptor("User", "string")
        };

        // Act
        var result = ServiceRegistryEmitter.Emit(repos);

        // Assert
        Assert.Contains("static get User(): UserService", result);
        Assert.Contains("new UserService(", result);
        Assert.Contains("`${config.baseUrl}/api/User`", result);
    }

    [Fact]
    public void Emit_WithCustomFactoryName_UsesFactoryNameForGetter()
    {
        // Arrange
        var repos = new[]
        {
            CreateRepositoryDescriptor("Calendar", "LeagueKey", factoryName: "SerieA")
        };

        // Act
        var result = ServiceRegistryEmitter.Emit(repos);

        // Assert
        Assert.Contains("static get SerieA(): SerieAService", result);
        Assert.Contains("`${config.baseUrl}/api/SerieA`", result);
    }

    [Fact]
    public void Emit_ExportsCommonTypes()
    {
        // Arrange
        var repos = new[]
        {
            CreateRepositoryDescriptor("User", "string")
        };

        // Act
        var result = ServiceRegistryEmitter.Emit(repos);

        // Assert
        Assert.Contains("export { Entity, State, BatchOperation, BatchCommand, QueryOptions, Page } from './common';", result);
    }

    [Fact]
    public void Emit_ContainsAutoGeneratedComment()
    {
        // Arrange
        var repos = new[]
        {
            CreateRepositoryDescriptor("User", "string")
        };

        // Act
        var result = ServiceRegistryEmitter.Emit(repos);

        // Assert
        Assert.Contains("Auto-generated by Rystem TypeScript Generator", result);
    }

    [Fact]
    public void GetFileName_ReturnsIndexTs()
    {
        // Act
        var fileName = ServiceRegistryEmitter.GetFileName();

        // Assert
        Assert.Equal("index.ts", fileName);
    }

    [Fact]
    public void Emit_ServiceGetterUsesLazyInitialization()
    {
        // Arrange
        var repos = new[]
        {
            CreateRepositoryDescriptor("User", "string")
        };

        // Act
        var result = ServiceRegistryEmitter.Emit(repos);

        // Assert
        Assert.Contains("if (!Services.instances.has('user'))", result);
        Assert.Contains("Services.instances.set('user'", result);
        Assert.Contains("return Services.instances.get('user') as UserService;", result);
    }
}
