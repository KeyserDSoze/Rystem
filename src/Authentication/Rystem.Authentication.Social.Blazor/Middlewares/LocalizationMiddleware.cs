using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class LocalizationMiddleware : IMiddleware
    {
        private const string LanguageCookieName = "lang";
        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Cookies.TryGetValue(LanguageCookieName, out var cookieLanguage))
            {
                var language = cookieLanguage;
                CultureInfo.CurrentCulture = new CultureInfo(language ?? "en");
                CultureInfo.CurrentUICulture = new CultureInfo(language ?? "en");
            }
            return next(context);
        }
    }
}
