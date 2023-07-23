namespace Microsoft.Extensions.DependencyInjection
{
    public interface IFactory<out TService>
    {
        TService Create(string? name = null);
    }
}
