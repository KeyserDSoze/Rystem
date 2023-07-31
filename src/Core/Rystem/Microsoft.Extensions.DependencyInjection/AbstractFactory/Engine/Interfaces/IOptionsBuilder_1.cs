namespace Microsoft.Extensions.DependencyInjection
{
    public interface IOptionsBuilder<out TService> : IOptionsBuilder
        where TService : class
    {
        Func<IServiceProvider, TService> Build();
    }
}
