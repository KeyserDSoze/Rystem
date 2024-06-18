namespace Microsoft.Extensions.DependencyInjection
{
    internal interface IActionFallback
    {
        Func<FallbackBuilderForServiceCollection, ValueTask> BuilderWithRebuilding { get; set; }
    }
}
