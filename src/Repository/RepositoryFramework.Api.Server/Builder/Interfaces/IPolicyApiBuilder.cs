using Microsoft.AspNetCore.Authorization;

namespace RepositoryFramework
{
    public interface IPolicyApiBuilder
    {
        IApiBuilder Builder { get; }
        IApiBuilder WithAuthorization(Action<AuthorizationOptions> authorizationOptions);
    }
}
