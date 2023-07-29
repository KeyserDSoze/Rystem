namespace Microsoft.Extensions.DependencyInjection
{
    public interface IDecoratedService<TService>
        where TService : class
    {
        TService Service { get; }
    }
}
