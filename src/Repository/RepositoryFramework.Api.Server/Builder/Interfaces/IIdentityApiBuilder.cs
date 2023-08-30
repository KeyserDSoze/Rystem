using Microsoft.AspNetCore.Authorization;

namespace RepositoryFramework
{
    public interface IIdentityApiBuilder
    {
        IApiBuilder Builder { get; }
        IApiBuilder WithAuthorization(Action<AuthorizationOptions> authorizationOptions);
    }
}
