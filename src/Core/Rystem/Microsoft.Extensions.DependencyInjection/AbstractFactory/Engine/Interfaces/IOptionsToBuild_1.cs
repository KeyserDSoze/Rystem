namespace Microsoft.Extensions.DependencyInjection
{
    public interface IOptionsToBuild<out TService> : IOptionsToBuild
        where TService : class
    {
        Func<IServiceProvider, TService> Build();
    }
}
