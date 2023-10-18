namespace Microsoft.Extensions.DependencyInjection
{
    public interface IFactory<out TService>
        where TService : class
    {
        TService? Create(string? name = null);
        TService? CreateWithoutDecoration(string? name = null);
        IEnumerable<TService> CreateAll(string? name = null);
        IEnumerable<TService> CreateAllWithoutDecoration(string? name = null);
        bool Exists(string? name = null);
    }
}
