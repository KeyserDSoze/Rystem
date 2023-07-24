namespace Microsoft.Extensions.DependencyInjection
{
    public interface IDecoratedService<TService>
        where TService : class
    {
        TService Service { get; }
        public static IDecoratedService<TService> Default<TImplementation>(TImplementation implementation)
            where TImplementation : class, TService
            => new DecoratedService<TService, TImplementation>(implementation);
    }
}
