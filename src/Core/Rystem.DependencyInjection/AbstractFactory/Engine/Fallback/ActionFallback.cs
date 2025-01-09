namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class ActionFallback<T> : IFactoryFallback<T>
        where T : class
    {
        private readonly IServiceProvider _serviceProvider;

        public ActionFallback(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public required Func<FallbackBuilderForServiceProvider, T> BuilderWithServiceProvider { get; init; }
        public T Create(AnyOf<string, Enum>? name = null)
        {
            return BuilderWithServiceProvider
               .Invoke(new FallbackBuilderForServiceProvider
               {
                   Name = name,
                   Services = _serviceProvider
               })!;
        }
    }
}
