using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Builder;

/// <summary>
/// Fluent builder for registering business hooks on a PlayFramework factory.
/// Accessed via <see cref="PlayFrameworkBuilder.Business"/>.
/// </summary>
public sealed class PlayFrameworkBusinessBuilder
{
    private readonly IServiceCollection _services;
    private readonly AnyOf<string?, Enum>? _name;

    // Tracks (hookInterface, hookImplementation, priority) in registration order.
    private readonly List<(Type HookInterface, Type HookImpl, int Priority)> _registrations = [];

    internal PlayFrameworkBusinessBuilder(IServiceCollection services, AnyOf<string?, Enum>? name)
    {
        _services = services;
        _name = name;
    }

    /// <summary>
    /// Registers a <see cref="IPlayFrameworkBeforeExecution"/> hook.
    /// Hooks run in ascending <paramref name="priority"/> order.
    /// </summary>
    public PlayFrameworkBusinessBuilder AddBeforeExecution<T>(int priority = 0)
        where T : class, IPlayFrameworkBeforeExecution
    {
        _services.AddFactory<IPlayFrameworkBeforeExecution, T>(_name, ServiceLifetime.Scoped);
        _registrations.Add((typeof(IPlayFrameworkBeforeExecution), typeof(T), priority));
        return this;
    }

    /// <summary>
    /// Registers a <see cref="IPlayFrameworkAfterEachScene"/> hook.
    /// Hooks run in ascending <paramref name="priority"/> order.
    /// </summary>
    public PlayFrameworkBusinessBuilder AddAfterEachScene<T>(int priority = 0)
        where T : class, IPlayFrameworkAfterEachScene
    {
        _services.AddFactory<IPlayFrameworkAfterEachScene, T>(_name, ServiceLifetime.Scoped);
        _registrations.Add((typeof(IPlayFrameworkAfterEachScene), typeof(T), priority));
        return this;
    }

    /// <summary>
    /// Registers a <see cref="IPlayFrameworkOnTerminalScene"/> hook.
    /// Hooks run in ascending <paramref name="priority"/> order.
    /// </summary>
    public PlayFrameworkBusinessBuilder AddOnTerminalScene<T>(int priority = 0)
        where T : class, IPlayFrameworkOnTerminalScene
    {
        _services.AddFactory<IPlayFrameworkOnTerminalScene, T>(_name, ServiceLifetime.Scoped);
        _registrations.Add((typeof(IPlayFrameworkOnTerminalScene), typeof(T), priority));
        return this;
    }

    /// <summary>
    /// Builds and registers the <see cref="PlayFrameworkHookRegistry"/> singleton and the
    /// <see cref="IPlayFrameworkBusinessManager"/> factory for this factory name.
    /// Called automatically by <see cref="ServiceCollectionExtensions.AddPlayFramework(Microsoft.Extensions.DependencyInjection.IServiceCollection,AnyOf{string?,Enum}?,Action{PlayFrameworkBuilder})"/>
    /// after the user's configure lambda has run.
    /// </summary>
    internal void BuildRegistry()
    {
        var registry = new PlayFrameworkHookRegistry();
        foreach (var (_, impl, priority) in _registrations)
        {
            registry.Register(impl, priority);
        }

        // Register registry as singleton factory-keyed (CreateAll not needed — single instance per name)
        _services.AddFactory(registry, _name, ServiceLifetime.Singleton);

        // Register business manager factory-keyed (Scoped — one per request)
        _services.AddFactory<IPlayFrameworkBusinessManager, PlayFrameworkBusinessManager>(_name, ServiceLifetime.Scoped);
    }
}
