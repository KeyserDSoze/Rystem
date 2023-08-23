namespace Microsoft.Extensions.DependencyInjection
{
    public interface IDecoratorService<in TService> : IServiceForFactory
        where TService : class
    {
        void SetDecoratedService(TService service);
    }
}
