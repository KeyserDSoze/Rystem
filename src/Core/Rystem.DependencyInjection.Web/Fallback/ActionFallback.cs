namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class ActionFallback<T> : IFactoryFallback<T>, IActionFallback
        where T : class
    {
        public Func<FallbackBuilderForServiceCollection, ValueTask>? BuilderWithRebuilding { get; set; }
        public T Create(string? name = null)
        {
            var fallbackBuilder = new FallbackBuilderForServiceCollection
            {
                Name = name,
                ServiceProvider = RuntimeServiceProvider.GetServiceProvider().CreateScope().ServiceProvider
            };
            if (BuilderWithRebuilding != null)
            {
                BuilderWithRebuilding
                   .Invoke(fallbackBuilder)
                   .ToResult();
                var serviceProvider = RuntimeServiceProvider
                   .AddServicesToServiceCollectionWithLock(fallbackBuilder.ServiceColletionBuilder)
                   .RebuildAsync()
                   .ToResult();
                return serviceProvider.GetService<IFactory<T>>()!.Create(name)!;
            }
            else
                return default!;
        }
    }
}
