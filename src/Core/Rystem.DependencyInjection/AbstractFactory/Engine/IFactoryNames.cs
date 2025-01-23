namespace Microsoft.Extensions.DependencyInjection
{
    public interface IFactoryNames
    {
        List<AnyOf<string, Enum>?> List();
    }
    public interface IFactoryNames<TService> : IFactoryNames
        where TService : class
    {
    }
}
