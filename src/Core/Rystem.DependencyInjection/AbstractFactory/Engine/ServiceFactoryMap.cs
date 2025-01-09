namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class ServiceFactoryMap
    {
        public Dictionary<object, List<ServiceDescriptor>> Services { get; } = [];
        public Dictionary<string, Action<object, object>> OptionsSetter { get; } = [];
        public Dictionary<string, int> DecorationCount { get; } = [];
    }
}
