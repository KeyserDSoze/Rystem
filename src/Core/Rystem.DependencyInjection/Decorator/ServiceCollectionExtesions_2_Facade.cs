namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDecoration<TService, TImplementation>(
            this IServiceCollection services,
            AnyOf<string?, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService, IDecoratorService<TService>
            => services.AddDecorationEngine<TService, TImplementation>(null, null, name, lifetime);
        public static IServiceCollection AddDecoration<TService, TImplementation>(
            this IServiceCollection services,
            TImplementation implementationInstance,
            AnyOf<string?, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService, IDecoratorService<TService>
            => services.AddDecorationEngine<TService, TImplementation>(implementationInstance, null, name, lifetime);
        public static IServiceCollection AddDecoration<TService, TImplementation>(
            this IServiceCollection services,
            Func<IServiceProvider, object?, TService> implementationFactory,
            AnyOf<string?, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService, IDecoratorService<TService>
            => services.AddDecorationEngine<TService, TImplementation>(null, implementationFactory, name, lifetime);
    }
}
