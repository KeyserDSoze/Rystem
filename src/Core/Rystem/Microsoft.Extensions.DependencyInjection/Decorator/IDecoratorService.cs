namespace Microsoft.Extensions.DependencyInjection
{
    public interface IDecoratorService<TService>
        where TService : class
    {
        void SetDecoratedService(TService service);
    }
}
