namespace Microsoft.Extensions.DependencyInjection
{
    public interface IFactory<out TService>
        where TService : class
    {
        TService? Create(AnyOf<string?, Enum>? name = null);
        TService? CreateWithoutDecoration(AnyOf<string?, Enum>? name = null);
        IEnumerable<TService> CreateAll(AnyOf<string?, Enum>? name = null);
        IEnumerable<TService> CreateAllWithoutDecoration(AnyOf<string?, Enum>? name = null);
        bool Exists(AnyOf<string?, Enum>? name = null);
    }
}
