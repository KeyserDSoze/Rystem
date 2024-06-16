namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class FallbackBuilderForServiceProvider
    {
        public IServiceProvider Services { get; internal init; } = null!;
        public string? Name { get; internal init; }
    }
}
