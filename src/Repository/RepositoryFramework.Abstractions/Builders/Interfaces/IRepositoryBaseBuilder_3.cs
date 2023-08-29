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
        void SetNotExposable();
        void SetExposable(RepositoryMethods methods = RepositoryMethods.All);
        void SetOnlyQueryExposable();
        void SetOnlyCommandExposable();
        void SetExamples(T entity, TKey key);
    }
}
