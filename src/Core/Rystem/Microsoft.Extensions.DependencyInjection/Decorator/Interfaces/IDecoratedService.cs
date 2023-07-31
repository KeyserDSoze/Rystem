namespace Microsoft.Extensions.DependencyInjection
{
    public interface IDecoratedService<out TService>
        where TService : class
    {
        TService Service { get; }
    }
}
