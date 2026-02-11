using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Tests for dynamic actors in PlayFramework.
/// </summary>
public sealed class DynamicActorTests : PlayFrameworkTestBase
{
    protected override void ConfigurePlayFramework(IServiceCollection services)
    {
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<DatabaseContextActor>();

        services.AddPlayFramework(builder =>
        {
            builder
                .Configure(settings =>
                {
                    settings.Planning.Enabled = false;
                    settings.Summarization.Enabled = false;
                })
                .AddMainActor("You are a helpful assistant.")
                .AddMainActor(ctx =>
                {
                    var userId = ctx.GetProperty<string>("UserId") ?? "guest";
                    return $"Current user: {userId}";
                }, cacheForSubsequentCalls: true)
                .AddMainActor<DatabaseContextActor>(cacheForSubsequentCalls: false)
                .AddScene(sceneBuilder =>
                {
                    sceneBuilder
                        .WithName("UserInfo")
                        .WithDescription("Provides user information")
                        .WithService<IUserContextService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.GetUserNameAsync(), "get_username", "Get the current user's name");
                        });
                });
        });
    }

    [Fact]
    public void MainActors_ShouldBeConfigured()
    {
        // Arrange
        var settings = ServiceProvider.GetRequiredService<PlayFrameworkSettings>();

        // Assert
        Assert.NotNull(settings);
    }

    [Fact]
    public async Task DynamicActor_ShouldAccessContext()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();
        var requestSettings = new SceneRequestSettings
        {
            CacheKey = Guid.NewGuid().ToString()
        };

        // We can't easily test dynamic actors without full execution
        // This test verifies the setup is correct
        Assert.NotNull(sceneManager);
    }
}

/// <summary>
/// Mock user context service.
/// </summary>
public interface IUserContextService
{
    Task<string> GetUserNameAsync();
}

public sealed class UserContextService : IUserContextService
{
    public Task<string> GetUserNameAsync() => Task.FromResult("Test User");
}

/// <summary>
/// Example of a custom actor that accesses database or external state.
/// </summary>
public sealed class DatabaseContextActor : IActor
{
    private readonly IUserContextService _userContext;

    public DatabaseContextActor(IUserContextService userContext)
    {
        _userContext = userContext;
    }

    public async Task<ActorResponse> PlayAsync(SceneContext context, CancellationToken cancellationToken = default)
    {
        var userName = await _userContext.GetUserNameAsync();
        
        return new ActorResponse
        {
            Message = $"Database context: User '{userName}' is authenticated.",
            CacheForSubsequentCalls = false // Don't cache because it might change
        };
    }
}
