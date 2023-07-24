namespace Microsoft.Extensions.DependencyInjection
{
    public interface IDecoratorService<TService>
        where TService : class
    {
        TService DecoratedService { get; set; }
        void OnDecoratedServiceSet(TService service);
    }
}
