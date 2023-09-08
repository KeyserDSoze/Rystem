namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDecoration<TService>(
           this IServiceCollection services,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IDecoratorService<TService>
            => services.AddDecorationEngine<TService>(default, default, name, lifetime);
        public static IServiceCollection AddDecoration<TService>(
           this IServiceCollection services,
           TService implementationInstance,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IDecoratorService<TService>
            => services.AddDecorationEngine<TService>(implementationInstance, default, name, lifetime);
        public static IServiceCollection AddDecoration<TService>(
           this IServiceCollection services,
           Func<IServiceProvider, object?, TService> implementationFactory,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IDecoratorService<TService>
            => services.AddDecorationEngine(default, implementationFactory, name, lifetime);
    }
}
