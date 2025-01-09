namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class ServiceFactoryMap
    {
        public Dictionary<object, List<ServiceDescriptor>> Services { get; } = new();
        public Dictionary<string, Action<object, object>> OptionsSetter { get; } = new();
        public Dictionary<string, int> DecorationCount { get; } = new();
    }
}
