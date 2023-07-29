namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
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
           Func<IServiceProvider, TService> implementationFactory,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IDecoratorService<TService>
            => services.AddDecorationEngine<TService>(default, implementationFactory, name, lifetime);
    }
}
