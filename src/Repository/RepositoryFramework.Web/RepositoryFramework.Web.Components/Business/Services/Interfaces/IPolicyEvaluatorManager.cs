using Microsoft.AspNetCore.Http;

namespace RepositoryFramework.Web.Components.Services
{
    public interface IPolicyEvaluatorManager
    {
        ValueTask<bool> ValidateAsync(HttpContext httpContext, List<string> policies);
    }
}
