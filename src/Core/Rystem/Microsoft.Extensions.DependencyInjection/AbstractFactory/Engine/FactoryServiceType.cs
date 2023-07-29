namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class FactoryServiceType
    {
        public Type? Type { get; set; }
        public int Index { get; set; }
        public string? Name { get; set; }
    }
}
