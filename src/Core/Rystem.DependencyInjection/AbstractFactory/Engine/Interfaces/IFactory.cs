namespace Microsoft.Extensions.DependencyInjection
{
    public interface IFactory<out TService>
        where TService : class
    {
        TService? Create(string? name = null);
        TService? CreateWithoutDecoration(string? name = null);
        bool Exists(string? name = null);
    }
}
