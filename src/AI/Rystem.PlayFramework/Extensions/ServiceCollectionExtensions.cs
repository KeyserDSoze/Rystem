using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Mcp;
using Rystem.PlayFramework.Services;
using Rystem.PlayFramework.Services.ExecutionModes;
using Rystem.PlayFramework.Services.Helpers;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for IServiceCollection to configure PlayFramework.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PlayFramework services to the DI container.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddPlayFramework(
        this IServiceCollection services,
        Action<PlayFrameworkBuilder> configure)
    {
        return AddPlayFramework(services, null, configure);
    }

    /// <summary>
    /// Adds PlayFramework services with a specific key for factory-based resolution.
    /// Use IFactory&lt;ISceneManager&gt; to resolve by key.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="name">Factory name (can be string or enum).</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddPlayFramework(
        this IServiceCollection services,
        AnyOf<string?, Enum>? name,
        Action<PlayFrameworkBuilder> configure)
    {
        var builder = new PlayFrameworkBuilder(services, name);
        configure(builder);

        // Register helper services (singleton, shared across all instances)
        services.TryAddSingleton<IResponseHelper, ResponseHelper>();
        services.TryAddSingleton<IStreamingHelper, StreamingHelper>();
        services.TryAddSingleton<ISceneMatchingHelper, SceneMatchingHelper>();
        services.TryAddSingleton<IClientInteractionHandler, ClientInteractionHandler>();

        // Ensure all IFactory<T> types that SceneManager depends on are registered
        // This allows them to be injected even when the service itself is not registered
        services.AddEngineFactory<ISceneFactory>();
        services.AddEngineFactory<IChatClient>();
        services.AddEngineFactory<IChatClientManager>();  // Add chat client manager factory
        services.AddEngineFactory<PlayFrameworkSettings>();
        services.AddEngineFactory<List<SceneConfiguration>>();
        services.AddEngineFactory<List<ActorConfiguration>>();
        services.AddEngineFactory<ICostCalculator>();
        services.AddEngineFactory<ITransientErrorDetector>();  // Add error detector factory
        services.AddEngineFactory<TokenCostSettings>();  // Add token cost settings factory
        services.AddEngineFactory<IPlanner>();
        services.AddEngineFactory<ISummarizer>();
        services.AddEngineFactory<IDirector>();
        services.AddEngineFactory<IPlayFrameworkCache>();
        services.AddEngineFactory<IJsonService>();
        services.AddEngineFactory<IMcpServerManager>();  // Add MCP server manager factory
        services.AddEngineFactory<IRateLimiter>();  // Add rate limiter factory (optional, but DI needs it registered)

        // Register PlayFrameworkCache as transient with factory pattern (supports named instances)
        if (!builder.HasCustomCache)
        {
            services.AddFactory<IPlayFrameworkCache>((sp, _) =>
            {
                var settingsFactory = sp.GetRequiredService<IFactory<PlayFrameworkSettings>>();
                var jsonServiceFactory = sp.GetRequiredService<IFactory<IJsonService>>();
                return new PlayFrameworkCache(
                    settingsFactory.Create(name) ?? new PlayFrameworkSettings(),
                    jsonServiceFactory.Create(name) ?? new DefaultJsonService(),
                    sp.GetRequiredService<ILogger<PlayFrameworkCache>>(),
                    sp.GetService<IDistributedCache>(),
                    sp.GetService<IMemoryCache>());
            }, name, ServiceLifetime.Transient);
        }

        // Register SceneManager with factory pattern
        services.AddFactory<ISceneManager, SceneManager>(name, ServiceLifetime.Transient);

        // Register SceneFactory with factory pattern (used by SceneManager)
        services.AddFactory<ISceneFactory>((sp, _) =>
        {
            var scenesFactory = sp.GetRequiredService<IFactory<List<SceneConfiguration>>>();
            var scenes = scenesFactory.Create(name) ?? [];
            return new SceneFactory(scenes, sp);
        }, name, ServiceLifetime.Transient);

        // Register Execution Mode infrastructure
        RegisterExecutionModeHandlers(services, name);

        // Register settings with factory pattern (Singleton) - using instance overload
        services.AddFactory(builder.Settings, name, ServiceLifetime.Singleton);

        // Register scenes configuration with factory pattern (Singleton)
        services.AddFactory(builder.Scenes, name, ServiceLifetime.Singleton);

        // Register main actors with factory pattern (Singleton)
        services.AddFactory(builder.MainActors, name, ServiceLifetime.Singleton);

        // Register cost calculator with factory pattern (Singleton)
        var costCalculator = builder.Settings.CostTracking.Enabled
            ? new CostCalculator(builder.Settings.CostTracking)
            : new CostCalculator(new TokenCostSettings { Enabled = false });
        services.AddFactory<ICostCalculator>(costCalculator, name, ServiceLifetime.Singleton);

        // Register default cost settings for direct IChatClient fallback
        services.AddFactory(builder.Settings.CostTracking, name, ServiceLifetime.Singleton);

        // Register default transient error detector if not customized
        if (!builder.HasCustomTransientErrorDetector)
        {
            services.AddFactory<ITransientErrorDetector, DefaultTransientErrorDetector>(
                name, ServiceLifetime.Singleton);
        }

        // Register rate limiting components if enabled
        if (builder.Settings.RateLimiting?.Enabled == true)
        {
            // Register storage (InMemory by default, unless Custom is specified)
            if (builder.Settings.RateLimiting.StorageType == RateLimitStorage.InMemory)
            {
                services.AddFactory<IRateLimitStorage, InMemoryRateLimitStorage>(
                    name, ServiceLifetime.Singleton);
            }
            // If Custom, user must register IRateLimitStorage separately

            // Register rate limiter based on strategy
            switch (builder.Settings.RateLimiting.Strategy)
            {
                case RateLimitingStrategy.TokenBucket:
                    services.AddFactory<IRateLimiter, TokenBucketRateLimiter>(name, ServiceLifetime.Singleton);
                    break;

                // Future strategies can be added here (SlidingWindow, FixedWindow, Concurrent)
                default:
                    throw new NotSupportedException($"Rate limiting strategy '{builder.Settings.RateLimiting.Strategy}' is not yet implemented. Use TokenBucket for now.");
            }
        }

        // Register unified chat client manager (handles load balancing, fallback, retry, and cost)
        services.AddFactory<IChatClientManager, ChatClientManager>(
            name, ServiceLifetime.Singleton);

        // Register default planner if not customized and planning is enabled
        if (!builder.HasCustomPlanner && builder.Settings.Planning.Enabled)
        {
            services.AddFactory<IPlanner, DeterministicPlanner>(name, ServiceLifetime.Transient);
        }

        // Register default summarizer if not customized and summarization is enabled
        if (!builder.HasCustomSummarizer && builder.Settings.Summarization.Enabled)
        {
            services.AddFactory<ISummarizer, DefaultSummarizer>(name, ServiceLifetime.Transient);
        }

        // Register default director if not customized and director is enabled
        if (!builder.HasCustomDirector && builder.Settings.Director.Enabled)
        {
            services.AddFactory<IDirector, MainDirector>(name, ServiceLifetime.Transient);
        }

        // Register default JSON service if not customized
        if (!builder.HasCustomJsonService)
        {
            services.AddFactory<IJsonService, DefaultJsonService>(name, ServiceLifetime.Singleton);
        }

        // Register memory components if enabled
        if (builder.Settings.Memory?.Enabled == true)
        {
            // Register memory settings
            services.AddFactory(builder.Settings.Memory, name, ServiceLifetime.Singleton);

            // Register memory storage (InMemory by default, unless Custom is specified)
            if (!builder.HasCustomMemoryStorage)
            {
                services.AddFactory<IMemoryStorage, InMemoryMemoryStorage>(name, ServiceLifetime.Singleton);
            }
            else if (builder.CustomMemoryStorageType != null)
            {
                services.AddFactory(typeof(IMemoryStorage), builder.CustomMemoryStorageType, name, ServiceLifetime.Singleton);
            }

            // Register memory service (default or custom)
            if (!builder.HasCustomMemory)
            {
                services.AddFactory<IMemory, Memory>(name, ServiceLifetime.Singleton);
            }
            else if (builder.CustomMemoryType != null)
            {
                services.AddFactory(typeof(IMemory), builder.CustomMemoryType, name, ServiceLifetime.Singleton);
            }

            // Register factory engines
            services.AddEngineFactory<IMemory>();
            services.AddEngineFactory<IMemoryStorage>();
            services.AddEngineFactory<MemorySettings>();
        }

        // Register actor types from scenes (Transient)
        foreach (var scene in builder.Scenes)
        {
            foreach (var actor in scene.Actors.Where(a => a.ActorType != null))
            {
                services.TryAddTransient(actor.ActorType!);
            }
        }

        // Register main actor types (Transient)
        foreach (var actor in builder.MainActors.Where(a => a.ActorType != null))
        {
            services.TryAddTransient(actor.ActorType!);
        }

        // Register IPlayFramework wrapper (always Transient)
        services.TryAddTransient<IPlayFramework, PlayFramework>();

        return services;
    }

    /// <summary>
    /// Registers all execution mode handlers with factory pattern.
    /// Each SceneExecutionMode enum value maps to a specific handler implementation.
    /// </summary>
    private static void RegisterExecutionModeHandlers(
        IServiceCollection services,
        AnyOf<string?, Enum>? name)
    {
        services.AddFactory<ExecutionModeHandlerDependencies>(name, ServiceLifetime.Transient);
        // Register shared dependencies
        services.AddFactory<ExecutionModeHandlerDependencies>((sp, factoryNameObj) =>
        {
            // Convert object to AnyOf<string?, Enum>?
            AnyOf<string?, Enum>? factoryName = factoryNameObj switch
            {
                null => null,
                string s => s,
                Enum e => e,
                _ => null
            };

            var settingsFactory = sp.GetRequiredService<IFactory<PlayFrameworkSettings>>();
            var sceneFactoryFactory = sp.GetRequiredService<IFactory<ISceneFactory>>();
            var responseHelper = sp.GetRequiredService<IResponseHelper>();
            var streamingHelper = sp.GetRequiredService<IStreamingHelper>();
            var sceneMatchingHelper = sp.GetRequiredService<ISceneMatchingHelper>();
            var playFrameworkCacheFactory = sp.GetRequiredService<IFactory<IPlayFrameworkCache>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return new ExecutionModeHandlerDependencies
            {
                SceneFactory = sceneFactoryFactory.Create(factoryName) 
                    ?? throw new InvalidOperationException($"SceneFactory not found for factory: {factoryName}"),
                ResponseHelper = responseHelper,
                StreamingHelper = streamingHelper,
                SceneMatchingHelper = sceneMatchingHelper,
                Settings = settingsFactory.Create(factoryName) ?? new PlayFrameworkSettings(),
                Logger = loggerFactory.CreateLogger<SceneManager>(),
                FactoryName = factoryName?.ToString() ?? "default"
            };
        }, name, ServiceLifetime.Transient);

        // Register SceneExecutor (shared by all handlers)
        services.AddFactory<ISceneExecutor>((sp, factoryNameObj) =>
        {
            // Convert object to AnyOf<string?, Enum>?
            AnyOf<string?, Enum>? factoryName = factoryNameObj switch
            {
                null => null,
                string s => s,
                Enum e => e,
                _ => null
            };

            var dependencies = sp.GetRequiredService<IFactory<ExecutionModeHandlerDependencies>>()
                .Create(factoryName)
                ?? throw new InvalidOperationException($"ExecutionModeHandlerDependencies not found for factory: {factoryName}");

            var mcpServerManagerFactory = sp.GetRequiredService<IFactory<IMcpServerManager>>();
            var jsonServiceFactory = sp.GetRequiredService<IFactory<IJsonService>>();
            var clientInteractionHandler = sp.GetRequiredService<IClientInteractionHandler>();

            return new SceneExecutor(
                dependencies,
                mcpServerManagerFactory,
                jsonServiceFactory,
                clientInteractionHandler,
                factoryName?.ToString() ?? "default");
        }, name, ServiceLifetime.Transient);

        // Register FinalResponseGenerator (shared by Planning and DynamicChaining)
        services.AddFactory<FinalResponseGenerator>((sp, factoryNameObj) =>
        {
            // Convert object to AnyOf<string?, Enum>?
            AnyOf<string?, Enum>? factoryName = factoryNameObj switch
            {
                null => null,
                string s => s,
                Enum e => e,
                _ => null
            };

            var dependencies = sp.GetRequiredService<IFactory<ExecutionModeHandlerDependencies>>()
                .Create(factoryName)
                ?? throw new InvalidOperationException($"ExecutionModeHandlerDependencies not found for factory: {factoryName}");

            return new FinalResponseGenerator(dependencies);
        }, name, ServiceLifetime.Transient);

        // Register execution mode handlers with enum keys
        // Direct mode
        services.AddFactory<IExecutionModeHandler>((sp, _) =>
        {
            var dependencies = sp.GetRequiredService<IFactory<ExecutionModeHandlerDependencies>>()
                .Create(name)
                ?? throw new InvalidOperationException($"ExecutionModeHandlerDependencies not found for factory: {name}");

            var sceneExecutor = sp.GetRequiredService<IFactory<ISceneExecutor>>()
                .Create(name)
                ?? throw new InvalidOperationException($"SceneExecutor not found for factory: {name}");

            return new DirectExecutionHandler(dependencies, sceneExecutor);
        }, SceneExecutionMode.Direct, ServiceLifetime.Transient);

        // Scene mode
        services.AddFactory<IExecutionModeHandler>((sp, _) =>
        {
            var dependencies = sp.GetRequiredService<IFactory<ExecutionModeHandlerDependencies>>()
                .Create(name)
                ?? throw new InvalidOperationException($"ExecutionModeHandlerDependencies not found for factory: {name}");

            var sceneExecutor = sp.GetRequiredService<IFactory<ISceneExecutor>>()
                .Create(name)
                ?? throw new InvalidOperationException($"SceneExecutor not found for factory: {name}");

            return new SceneExecutionHandler(dependencies, sceneExecutor);
        }, SceneExecutionMode.Scene, ServiceLifetime.Transient);

        // Planning mode
        services.AddFactory<IExecutionModeHandler>((sp, _) =>
        {
            var dependencies = sp.GetRequiredService<IFactory<ExecutionModeHandlerDependencies>>()
                .Create(name)
                ?? throw new InvalidOperationException($"ExecutionModeHandlerDependencies not found for factory: {name}");

            var sceneExecutor = sp.GetRequiredService<IFactory<ISceneExecutor>>()
                .Create(name)
                ?? throw new InvalidOperationException($"SceneExecutor not found for factory: {name}");

            var finalResponseGenerator = sp.GetRequiredService<IFactory<FinalResponseGenerator>>()
                .Create(name)
                ?? throw new InvalidOperationException($"FinalResponseGenerator not found for factory: {name}");

            var plannerFactory = sp.GetRequiredService<IFactory<IPlanner>>();
            var planner = plannerFactory.Create(name);

            return new PlanningExecutionHandler(dependencies, sceneExecutor, finalResponseGenerator, planner);
        }, SceneExecutionMode.Planning, ServiceLifetime.Transient);

        // DynamicChaining mode
        services.AddFactory<IExecutionModeHandler>((sp, _) =>
        {
            var dependencies = sp.GetRequiredService<IFactory<ExecutionModeHandlerDependencies>>()
                .Create(name)
                ?? throw new InvalidOperationException($"ExecutionModeHandlerDependencies not found for factory: {name}");

            var sceneExecutor = sp.GetRequiredService<IFactory<ISceneExecutor>>()
                .Create(name)
                ?? throw new InvalidOperationException($"SceneExecutor not found for factory: {name}");

            var finalResponseGenerator = sp.GetRequiredService<IFactory<FinalResponseGenerator>>()
                .Create(name)
                ?? throw new InvalidOperationException($"FinalResponseGenerator not found for factory: {name}");

            return new DynamicChainingExecutionHandler(
                dependencies, 
                sceneExecutor, 
                finalResponseGenerator,
                name?.ToString() ?? "default");
        }, SceneExecutionMode.DynamicChaining, ServiceLifetime.Transient);

        // Register engine factory for IExecutionModeHandler to allow resolution by enum
        services.AddEngineFactory<IExecutionModeHandler>();
    }
}

