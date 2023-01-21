using Microsoft.AspNetCore.Routing;

namespace RepositoryFramework
{
    /// <summary>
    /// Mapping for authorization in your auto-implemented api.
    /// You may set build your custom authorization (for example only for Insert and Update),
    /// and you may set the Policies that must be met.
    /// </summary>
    internal sealed class ApiAuthorizationBuilder : IApiAuthorizationBuilder
    {
        private readonly Func<ApiAuthorization?, IEndpointRouteBuilder> _finalizator;
        internal ApiAuthorization Authorization { get; } = new();
        internal ApiAuthorizationBuilder(Func<ApiAuthorization?, IEndpointRouteBuilder> finalizator)
            => _finalizator = finalizator;
        /// <summary>
        /// Set authorization with no policies.
        /// </summary>
        /// <returns>IEndpointRouteBuilder</returns>
        public IEndpointRouteBuilder WithDefaultAuthorization()
        {
            _ = SetPolicyForAll();
            return Build();
        }
        /// <summary>
        /// Set policies for a specific repository method.
        /// </summary>
        /// <returns>IEndpointRouteBuilder</returns>
        public IApiAuthorizationPolicy SetPolicy(params RepositoryMethods[] methods)
        {
            foreach (var method in methods)
                Authorization.Policies.Add(method, new());
            return new ApiAuthorizationPolicy(this, methods);
        }
        /// <summary>
        /// Set policies for command repository methods.
        /// </summary>
        /// <returns>IEndpointRouteBuilder</returns>
        public IApiAuthorizationPolicy SetPolicyForCommand()
        {
            Authorization.Policies.Add(RepositoryMethods.Insert, new());
            Authorization.Policies.Add(RepositoryMethods.Update, new());
            Authorization.Policies.Add(RepositoryMethods.Delete, new());
            Authorization.Policies.Add(RepositoryMethods.Batch, new());
            return new ApiAuthorizationPolicy(this, RepositoryMethods.Insert, RepositoryMethods.Update,
                RepositoryMethods.Delete, RepositoryMethods.Batch);
        }
        /// <summary>
        /// Set policies for query repository methods.
        /// </summary>
        /// <returns>IEndpointRouteBuilder</returns>
        public IApiAuthorizationPolicy SetPolicyForQuery()
        {
            Authorization.Policies.Add(RepositoryMethods.Exist, new());
            Authorization.Policies.Add(RepositoryMethods.Get, new());
            Authorization.Policies.Add(RepositoryMethods.Query, new());
            Authorization.Policies.Add(RepositoryMethods.Operation, new());
            return new ApiAuthorizationPolicy(this, RepositoryMethods.Exist, RepositoryMethods.Get,
                RepositoryMethods.Query, RepositoryMethods.Operation);
        }
        /// <summary>
        /// Set policies one time for every repository method.
        /// </summary>
        /// <returns>ApiAuthorizationPolicy</returns>
        public IApiAuthorizationPolicy SetPolicyForAll()
        {
            Authorization.Policies.Add(RepositoryMethods.All, new());
            return new ApiAuthorizationPolicy(this, RepositoryMethods.All);
        }
        /// <summary>
        /// Confirm the authorization policies created till now.
        /// </summary>
        /// <returns>IEndpointRouteBuilder</returns>
        public IEndpointRouteBuilder Build()
            => _finalizator.Invoke(Authorization);
        /// <summary>
        /// Remove authentication/authorization from api.
        /// </summary>
        /// <returns>IEndpointRouteBuilder</returns>
        public IEndpointRouteBuilder WithNoAuthorization()
            => _finalizator.Invoke(null);
    }
}
