namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class ActionFallback<T> : IFactoryFallback<T>
        where T : class
    {
        public required Func<FallbackBuilderForServiceCollection, ValueTask> BuilderWithRebuilding { get; init; }
        public T Create(string? name = null)
        {
            var serviceCollection = RuntimeServiceProvider.GetServiceCollection();
            BuilderWithRebuilding
               .Invoke(new FallbackBuilderForServiceCollection
               {
                   Name = name,
                   Services = serviceCollection,
                   ServiceProvider = RuntimeServiceProvider.GetServiceProvider().CreateScope().ServiceProvider
               })
               .ToResult();

            var serviceProvider = serviceCollection
               .ReBuildAsync()
               .ToResult();
            return serviceProvider.GetService<IFactory<T>>()!.Create(name)!;
        }
    }
}
