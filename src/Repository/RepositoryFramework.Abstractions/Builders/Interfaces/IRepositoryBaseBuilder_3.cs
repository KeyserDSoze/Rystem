using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    public interface IRepositoryBaseBuilder<T, TKey, TRepositoryBuilder>
        where TKey : notnull
        where TRepositoryBuilder : IRepositoryBaseBuilder<T, TKey, TRepositoryBuilder>
    {
        IServiceCollection Services { get; }
        QueryTranslationBuilder<T, TKey, TTranslated, TRepositoryBuilder> Translate<TTranslated>();
        RepositoryBusinessBuilder<T, TKey> AddBusiness(ServiceLifetime? serviceLifetime = null);
    }
}
