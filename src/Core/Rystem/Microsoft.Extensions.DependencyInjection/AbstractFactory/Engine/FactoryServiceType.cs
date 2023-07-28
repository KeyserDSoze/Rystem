namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class FactoryServiceType
    {
        public Type Type { get; set; } = null!;
        public int Index { get; set; }
    }
}
