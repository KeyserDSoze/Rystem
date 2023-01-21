using Microsoft.AspNetCore.Routing;

namespace RepositoryFramework
{
    internal sealed class ApiAuthorizationPolicy : IApiAuthorizationPolicy
    {
        private readonly RepositoryMethods[] _methods;
        private readonly ApiAuthorizationBuilder _authorizationBuilder;
        public ApiAuthorizationPolicy(ApiAuthorizationBuilder authorization, params RepositoryMethods[] methods)
        {
            _methods = methods;
            _authorizationBuilder = authorization;
        }
        public IApiAuthorizationPolicy With(params string[] policies)
        {
            foreach (var policy in policies)
                foreach (var method in _methods)
                    if (!_authorizationBuilder.Authorization.Policies[method].Contains(policy))
                        _authorizationBuilder.Authorization.Policies[method].Add(policy);
            return this;
        }
        public IApiAuthorizationBuilder Empty()
        {
            foreach (var method in _methods)
                _authorizationBuilder.Authorization.Policies[method] = new();
            return _authorizationBuilder;
        }
        public IApiAuthorizationBuilder And()
            => _authorizationBuilder;
        public IEndpointRouteBuilder Build()
            => _authorizationBuilder.Build();
    }
}
