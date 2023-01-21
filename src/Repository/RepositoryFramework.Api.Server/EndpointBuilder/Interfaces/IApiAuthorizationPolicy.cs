using Microsoft.AspNetCore.Routing;

namespace RepositoryFramework
{
    public interface IApiAuthorizationPolicy
    {
        IApiAuthorizationPolicy With(params string[] policies);
        IEndpointRouteBuilder Build();
        IApiAuthorizationBuilder Empty();
        IApiAuthorizationBuilder And();
    }
}
