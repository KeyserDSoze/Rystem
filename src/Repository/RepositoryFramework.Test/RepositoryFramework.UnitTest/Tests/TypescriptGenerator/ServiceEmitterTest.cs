using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.Services;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using Xunit;
using static RepositoryFramework.UnitTest.TypescriptGenerator.TestDescriptorFactory;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

/// <summary>
/// Tests for ServiceEmitter - generating TypeScript service classes.
/// </summary>
public class ServiceEmitterTest
{
    private static EmitterContext CreateContext() => new();

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
    public void Emit_Repository_GeneratesServiceClass()
    {
        // Arrange
        var model = CreateModel("User", CreateProperty("Id", NumberType));
        var repo = CreateRepositoryDescriptor("User", "string", RepositoryKind.Repository);
        var context = CreateContext();

        // Act
        var result = ServiceEmitter.Emit(repo, model, null, context);

        // Assert
        Assert.Contains("export class UserService", result);
        Assert.Contains("private baseUrl: string;", result);
        Assert.Contains("constructor(baseUrl: string", result);
    }

    [Fact]
    public void Emit_Repository_GeneratesAllMethods()
    {
        // Arrange
        var model = CreateModel("User", CreateProperty("Id", NumberType));
        var repo = CreateRepositoryDescriptor("User", "string", RepositoryKind.Repository);
        var context = CreateContext();

        // Act
        var result = ServiceEmitter.Emit(repo, model, null, context);

        // Assert
        // Query methods
        Assert.Contains("async get(key:", result);
        Assert.Contains("async exist(key:", result);
        Assert.Contains("async query(", result);
        Assert.Contains("async count(", result);
        
        // Command methods
        Assert.Contains("async insert(key:", result);
        Assert.Contains("async update(key:", result);
        Assert.Contains("async delete(key:", result);
        Assert.Contains("async batch(", result);
    }

    [Fact]
    public void Emit_QueryOnly_OnlyGeneratesQueryMethods()
    {
        // Arrange
        var model = CreateModel("User", CreateProperty("Id", NumberType));
        var repo = CreateRepositoryDescriptor("User", "string", RepositoryKind.Query);
        var context = CreateContext();

        // Act
        var result = ServiceEmitter.Emit(repo, model, null, context);

        // Assert
        // Query methods present
        Assert.Contains("async get(key:", result);
        Assert.Contains("async exist(key:", result);
        Assert.Contains("async query(", result);
        
        // Command methods absent
        Assert.DoesNotContain("async insert(", result);
        Assert.DoesNotContain("async update(", result);
        Assert.DoesNotContain("async delete(", result);
    }

    [Fact]
    public void Emit_CommandOnly_OnlyGeneratesCommandMethods()
    {
        // Arrange
        var model = CreateModel("User", CreateProperty("Id", NumberType));
        var repo = CreateRepositoryDescriptor("User", "string", RepositoryKind.Command);
        var context = CreateContext();

        // Act
        var result = ServiceEmitter.Emit(repo, model, null, context);

        // Assert
        // Command methods present
        Assert.Contains("async insert(key:", result);
        Assert.Contains("async update(key:", result);
        Assert.Contains("async delete(key:", result);
        Assert.Contains("async batch(", result);
        
        // Query methods absent
        Assert.DoesNotContain("async get(", result);
        Assert.DoesNotContain("async exist(", result);
    }

    [Fact]
    public void Emit_WithPrimitiveStringKey_GeneratesStringKeyType()
    {
        // Arrange
        var model = CreateModel("User", CreateProperty("Id", NumberType));
        var repo = CreateRepositoryDescriptor("User", "string");
        var context = CreateContext();

        // Act
        var result = ServiceEmitter.Emit(repo, model, null, context);

        // Assert
        Assert.Contains("async get(key: string)", result);
    }

    [Fact]
    public void Emit_WithPrimitiveIntKey_GeneratesNumberKeyType()
    {
        // Arrange
        var model = CreateModel("User", CreateProperty("Id", NumberType));
        var repo = CreateRepositoryDescriptor("User", "int");
        var context = CreateContext();

        // Act
        var result = ServiceEmitter.Emit(repo, model, null, context);

        // Assert
        Assert.Contains("async get(key: number)", result);
    }

    [Fact]
    public void Emit_WithComplexKey_GeneratesKeyType()
    {
        // Arrange
        var model = CreateModel("Calendar", CreateProperty("Year", NumberType));
        var key = CreateModel("LeagueKey", CreateProperty("LeagueId", NumberType));
        var repo = CreateRepositoryDescriptor("Calendar", "LeagueKey");
        var context = CreateContext();

        // Act
        var result = ServiceEmitter.Emit(repo, model, key, context);

        // Assert
        Assert.Contains("async get(key: LeagueKey)", result);
    }

    [Fact]
    public void Emit_WithCustomFactoryName_UsesFactoryName()
    {
        // Arrange
        var model = CreateModel("Calendar", CreateProperty("Year", NumberType));
        var repo = CreateRepositoryDescriptor("Calendar", "string", factoryName: "SerieACalendar");
        var context = CreateContext();

        // Act
        var result = ServiceEmitter.Emit(repo, model, null, context);

        // Assert
        Assert.Contains("export class SerieACalendarService", result);
    }

    [Fact]
    public void Emit_WithModelRequiringRaw_UsesMappers()
    {
        // Arrange
        var model = CreateModel("User",
            CreateProperty("Name", "n", StringType));
        var repo = CreateRepositoryDescriptor("User", "string");
        var context = CreateContext();
        context.TypesRequiringRaw.Add("User");

        // Act
        var result = ServiceEmitter.Emit(repo, model, null, context);

        // Assert
        Assert.Contains("mapRawUserToUser", result);
        Assert.Contains("mapUserToRawUser", result);
    }

    [Fact]
    public void Emit_GeneratesSerializeKeyMethod()
    {
        // Arrange
        var model = CreateModel("User", CreateProperty("Id", NumberType));
        var repo = CreateRepositoryDescriptor("User", "string");
        var context = CreateContext();

        // Act
        var result = ServiceEmitter.Emit(repo, model, null, context);

        // Assert
        Assert.Contains("private serializeKey(key:", result);
        Assert.Contains("encodeURIComponent", result);
    }

    [Fact]
    public void Emit_GeneratesRequestMethod()
    {
        // Arrange
        var model = CreateModel("User", CreateProperty("Id", NumberType));
        var repo = CreateRepositoryDescriptor("User", "string");
        var context = CreateContext();

        // Act
        var result = ServiceEmitter.Emit(repo, model, null, context);

        // Assert
        Assert.Contains("private async request<TResponse>", result);
        Assert.Contains("fetch(`${this.baseUrl}/", result);
        Assert.Contains("'Content-Type': 'application/json'", result);
    }

    [Fact]
    public void Emit_InsertMethod_PostsToInsertEndpoint()
    {
        // Arrange
        var model = CreateModel("User", CreateProperty("Id", NumberType));
        var repo = CreateRepositoryDescriptor("User", "string");
        var context = CreateContext();

        // Act
        var result = ServiceEmitter.Emit(repo, model, null, context);

        // Assert
        Assert.Contains("'Insert'", result);
        Assert.Contains("'POST'", result);
    }

    [Fact]
    public void Emit_BatchMethod_AcceptsBatchOperations()
    {
        // Arrange
        var model = CreateModel("User", CreateProperty("Id", NumberType));
        var repo = CreateRepositoryDescriptor("User", "string");
        var context = CreateContext();

        // Act
        var result = ServiceEmitter.Emit(repo, model, null, context);

        // Assert
        Assert.Contains("async batch(operations: BatchOperation<User, string>[])", result);
    }
}
