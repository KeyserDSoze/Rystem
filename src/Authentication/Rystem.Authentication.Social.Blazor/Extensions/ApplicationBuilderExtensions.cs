using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSocialLoginAuthorization(this IApplicationBuilder app)
        {
            app.UseMiddleware<LocalizationMiddleware>();
            return app;
        }
    }
}
