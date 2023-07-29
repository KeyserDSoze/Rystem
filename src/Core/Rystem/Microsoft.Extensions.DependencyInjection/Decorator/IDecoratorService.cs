namespace Microsoft.Extensions.DependencyInjection
{
    public interface IDecoratorService<in TService>
        where TService : class
    {
        void SetDecoratedService(TService service);
    }
}
