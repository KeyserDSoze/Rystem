namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static IServiceCollection AddDecoration<TService, TImplementation>(
            this IServiceCollection services,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService, IDecoratorService<TService>
            => services.AddDecorationEngine<TService, TImplementation>(null, null, name, lifetime);
        public static IServiceCollection AddDecoration<TService, TImplementation>(
            this IServiceCollection services,
            TImplementation implementationInstance,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService, IDecoratorService<TService>
            => services.AddDecorationEngine<TService, TImplementation>(implementationInstance, null, name, lifetime);
        public static IServiceCollection AddDecoration<TService, TImplementation>(
            this IServiceCollection services,
            Func<IServiceProvider, object?, TService> implementationFactory,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService, IDecoratorService<TService>
            => services.AddDecorationEngine<TService, TImplementation>(null, implementationFactory, name, lifetime);
    }
}
