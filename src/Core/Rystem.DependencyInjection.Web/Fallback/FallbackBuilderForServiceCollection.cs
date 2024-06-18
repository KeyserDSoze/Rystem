namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class FallbackBuilderForServiceCollection
    {
        public Action<IServiceCollection> ServiceColletionBuilder { get; set; } = AddEmpty;
        public IServiceProvider ServiceProvider { get; internal init; } = null!;
        public string? Name { get; internal init; }
        private static void AddEmpty(IServiceCollection services) { }
    }
}
