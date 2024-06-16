namespace Microsoft.Extensions.DependencyInjection
{
    public interface IFactoryFallback<out TService>
    {
        TService Create(string? name = null);
    }
}
