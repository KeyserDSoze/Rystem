namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class FactoryService<TService>
    {
        public Func<IServiceProvider, bool, TService> ServiceFactory { get; set; } = null!;
        public FactoryServiceType Implementation { get; set; } = null!;
        public List<FactoryServiceType>? Decorators { get; set; }
        public Dictionary<string, Func<IServiceProvider, TService, TService>> FurtherBehaviors { get; set; } = new();
    }
}
