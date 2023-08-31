namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class ScanResult
    {
        public int Count => Implementations.Count;
        public required List<Type> Implementations { get; set; }
    }
}
