using System.Net;
using Microsoft.AspNetCore.Routing;

namespace RepositoryFramework
{
    public interface IApiAuthorizationBuilder
    {
        /// <summary>
        /// Set authorization with no policies.
        /// </summary>
        /// <returns>IEndpointRouteBuilder</returns>
        IEndpointRouteBuilder WithDefaultAuthorization();
        /// <summary>
        /// Set policies for a specific repository method.
        /// </summary>
        /// <returns>IEndpointRouteBuilder</returns>
        IApiAuthorizationPolicy SetPolicy(params RepositoryMethods[] methods);
        /// <summary>
        /// Set policies for command repository methods.
        /// </summary>
        /// <returns>IEndpointRouteBuilder</returns>
        IApiAuthorizationPolicy SetPolicyForCommand();
        /// <summary>
        /// Set policies for query repository methods.
        /// </summary>
        /// <returns>IEndpointRouteBuilder</returns>
        IApiAuthorizationPolicy SetPolicyForQuery();
        /// <summary>
        /// Set policies one time for every repository method.
        /// </summary>
        /// <returns>ApiAuthorizationPolicy</returns>
        IApiAuthorizationPolicy SetPolicyForAll();
        /// <summary>
        /// Confirm the authorization policies created till now.
        /// </summary>
        /// <returns>IEndpointRouteBuilder</returns>
        IEndpointRouteBuilder Build();
        /// <summary>
        /// Remove authentication/authorization from api.
        /// </summary>
        /// <returns>IEndpointRouteBuilder</returns>
        IEndpointRouteBuilder WithNoAuthorization();
    }
}
