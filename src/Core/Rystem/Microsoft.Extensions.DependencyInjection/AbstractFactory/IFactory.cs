namespace Microsoft.Extensions.DependencyInjection
{
    public interface IFactory<out T>
    {
        T Create(string? name = null);
    }
}
