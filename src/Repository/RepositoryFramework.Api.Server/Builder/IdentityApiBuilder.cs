using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework
{
    internal sealed class IdentityApiBuilder : IIdentityApiBuilder
    {
        public IApiBuilder Builder { get; }
        public IdentityApiBuilder(IApiBuilder apiBuilder)
            => Builder = apiBuilder;

        public IApiBuilder WithAuthorization(Action<AuthorizationOptions> authorizationOptions)
        {
            Builder.Services.AddAuthorization(authorizationOptions);
            return Builder;
        }
    }
}
