namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class FallbackBuilderForServiceCollection
    {
        public Action<IServiceCollection> ServiceColletionBuilder { get; set; } = AddEmpty;
        public IServiceProvider ServiceProvider { get; internal init; } = null!;
        public AnyOf<string, Enum>? Name { get; internal init; }
        private static void AddEmpty(IServiceCollection services) { }
    }
}
