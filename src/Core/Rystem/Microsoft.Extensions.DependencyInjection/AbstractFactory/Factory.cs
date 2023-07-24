namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class Factory<TService> : IFactory<TService>
    {
        private readonly IServiceProvider _serviceProvider;
        public Factory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public TService Create(string? name = null)
            => Map[name ?? string.Empty].ServiceFactory.Invoke(_serviceProvider, false);
        public TService CreateWithoutDecoration(string? name = null)
            => Map[name ?? string.Empty].ServiceFactory.Invoke(_serviceProvider, true);
        public bool Exists(string? name = null)
            => Map.ContainsKey(name ?? string.Empty);
        internal static Dictionary<string, FactoryService> Map { get; } = new();
        internal sealed class FactoryService
        {
            public Func<IServiceProvider, bool, TService> ServiceFactory { get; set; } = null!;
            public Type ImplementationType { get; set; } = null!;
            public List<Type>? DecoratorTypes { get; set; }
            public Dictionary<string, Func<IServiceProvider, TService, TService>> FurtherBehaviors { get; set; } = new();
        }
    }
}
