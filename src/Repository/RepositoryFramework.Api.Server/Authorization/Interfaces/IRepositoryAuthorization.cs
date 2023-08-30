using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace RepositoryFramework.Api.Server.Authorization
{
    public interface IRepositoryAuthorization<in T, in TKey>
        where TKey : notnull
    {
        Task<AuthorizedRepositoryResponse> HandleRequirementAsync(IHttpContextAccessor httpContextAccessor,
            AuthorizationHandlerContext context,
            RepositoryRequirement requirement,
            RepositoryMethods method,
            TKey? key,
            T? value);
    }
}
