namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceOptions<out T> : IServiceOptions
        where T : class
    {
        Func<T> Build();
    }
}
