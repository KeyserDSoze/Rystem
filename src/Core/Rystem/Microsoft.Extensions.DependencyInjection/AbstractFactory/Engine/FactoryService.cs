namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class FactoryService<TService>
    {
        public Func<IServiceProvider, bool, TService> ServiceFactory { get; internal set; } = null!;
        public ServiceDescriptor Descriptor { get; internal set; } = null!;
        public List<ServiceDescriptor>? Decorators { get; internal set; }
        public Dictionary<string, Func<IServiceProvider, TService, TService>> FurtherBehaviors { get; set; } = new();
    }
}
