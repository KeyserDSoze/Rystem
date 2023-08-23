namespace Microsoft.Extensions.DependencyInjection
{
    public interface IFactory<out TService>
    {
        TService Create(string? name = null);
        TService CreateWithoutDecoration(string? name = null);
        bool Exists(string? name = null);
    }
}
