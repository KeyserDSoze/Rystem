namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class FactoryNames<TService> : IFactoryNames<TService>
        where TService : class
    {
        public List<AnyOf<string, Enum>?> Names { get; } = [];
        public List<AnyOf<string, Enum>?> List()
            => Names;
    }
}
