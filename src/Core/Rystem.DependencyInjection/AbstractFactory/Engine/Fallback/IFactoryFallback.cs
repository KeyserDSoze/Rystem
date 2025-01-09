namespace Microsoft.Extensions.DependencyInjection
{
    public interface IFactoryFallback<out TService>
    {
        TService Create(AnyOf<string, Enum>? name = null);
    }
}
