namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class FactoryServices<TService>
    {
        public Dictionary<string, FactoryService<TService>> Services { get; } = new();
    }
}
