namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class FallbackBuilderForServiceCollection
    {
        public IServiceCollection Services { get; internal init; } = null!;
        public IServiceProvider ServiceProvider { get; internal init; } = null!;
        public string? Name { get; internal init; }
    }
}
