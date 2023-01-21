using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    internal sealed class PolicyApiBuilder : IPolicyApiBuilder
    {
        public IApiBuilder Builder { get; }
        public PolicyApiBuilder(IApiBuilder apiBuilder)
            => Builder = apiBuilder;

        public IApiBuilder WithAuthorization(Action<AuthorizationOptions> authorizationOptions)
        {
            Builder.Services.AddAuthorization(authorizationOptions);
            return Builder;
        }
    }
}
