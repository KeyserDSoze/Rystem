namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class FallbackBuilderForServiceProvider
    {
        public IServiceProvider Services { get; internal init; } = null!;
        public AnyOf<string?, Enum>? Name { get; internal init; }
    }
}
