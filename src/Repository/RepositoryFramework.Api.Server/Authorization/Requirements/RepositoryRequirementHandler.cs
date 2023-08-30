using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace RepositoryFramework.Api.Server.Authorization
{
    public class RepositoryRequirementHandler : AuthorizationHandler<RepositoryRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public RepositoryRequirementHandler(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
            RepositoryRequirement requirement)
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                var handleService = _serviceProvider.GetService(requirement.Type);
                if (handleService != null)
                {
                    var entity = await requirement.EntityReader(_httpContextAccessor);
                    var authResponseAsTask = requirement.Handler.Invoke(handleService, new object[]
                    {
                        _httpContextAccessor,
                        context,
                        requirement,
                        entity.Method,
                        entity.Key!,
                        entity.Value!
                    }) as Task<AuthorizedRepositoryResponse>;
                    var authResponse = await authResponseAsTask!;
                    if (!authResponse.Success)
                    {
                        context.Fail(new AuthorizationFailureReason(this, authResponse.Message ?? requirement.PolicyName));
                        return;
                    }
                }
            }
            context.Succeed(requirement);
        }
    }
}
