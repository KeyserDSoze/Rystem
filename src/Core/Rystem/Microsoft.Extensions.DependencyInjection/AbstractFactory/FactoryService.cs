namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class FactoryService<TService>
    {
        public Func<IServiceProvider, bool, TService> ServiceFactory { get; set; } = null!;
        public Type ImplementationType { get; set; } = null!;
        public List<Type>? DecoratorTypes { get; set; }
        public Dictionary<string, Func<IServiceProvider, TService, TService>> FurtherBehaviors { get; set; } = new();
    }
}
