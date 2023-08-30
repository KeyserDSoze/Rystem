using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.Api.Server.Authorization
{
    public interface IRepositoryPolicyBuilder<T, TKey>
        where TKey : notnull
    {
        IRepositoryPolicyBuilder<T, TKey> WithAuthorizationHandler<THandler>(ServiceLifetime lifetime = ServiceLifetime.Transient)
            where THandler : class, IRepositoryAuthorization<T, TKey>;
    }
}
