using System.Globalization;
using RepositoryFramework;
using RepositoryFramework.InMemory;

namespace Rystem.Localization.Test.App
{
    public static class ServiceCollectionExtensions
    {
        public sealed class LocalizationMiddleware : IMiddleware
        {
            private readonly IRepository<Userone, string> _userRepository;

            public LocalizationMiddleware(IRepository<Userone, string> userRepository)
            {
                _userRepository = userRepository;
            }
            public async Task InvokeAsync(HttpContext context, RequestDelegate next)
            {
                if (context.Request.Cookies.TryGetValue("lang", out var cookieLanguage))
                {
                    var language = cookieLanguage;
                    CultureInfo.CurrentCulture = new CultureInfo(language ?? "en");
                    CultureInfo.CurrentUICulture = new CultureInfo(language ?? "en");
                }
                //else if (context.Request.Query.TryGetValue("key", out var key))
                //{
                //    var userId = key.First();
                //    var user = await _userRepository.GetAsync(userId);
                //    CultureInfo.CurrentCulture = new CultureInfo(user.Language ?? "en");
                //    CultureInfo.CurrentUICulture = new CultureInfo(user.Language ?? "en");
                //    context.Response.Cookies.Append("language", user.Language ?? "en", new CookieOptions
                //    {
                //        Expires = DateTimeOffset.UtcNow.AddDays(30),
                //        HttpOnly = true,
                //        Secure = context.Request.IsHttps
                //    });
                //}
                await next(context);
            }
        }
        public static IApplicationBuilder AddLocalizationMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<LocalizationMiddleware>();
            return app;
        }
        public static IServiceCollection AddLocalizationForRystem(this IServiceCollection services)
        {
            services.AddRepository<Userone, string>(x => x.WithInMemory());
            services.AddWarmUp(async (serviceProvider) =>
            {
                var repository = serviceProvider.GetRequiredService<IRepository<Userone, string>>();
                await repository.InsertAsync("1", new Userone { Language = null });
                await repository.InsertAsync("2", new Userone { Language = "it" });
            });
            services.AddLocalizationWithRepositoryFramework<TheDictionary>(builder =>
            {
                builder.WithInMemory(name: "localization");
            },
            "localization",
            async (serviceProvider) =>
            {
                var repository = serviceProvider.GetRequiredService<IRepository<TheDictionary, string>>();
                await repository.InsertAsync("it", new TheDictionary
                {
                    Value = "Valore",
                    TheFirstPage = new TheFirstPage
                    {
                        Title = "Titolo",
                        Description = "Descrizione"
                    },
                    TheSecondPage = new TheSecondPage
                    {
                        Title = "Titolo {0}",
                    }
                });
                await repository.InsertAsync("en", new TheDictionary
                {
                    Value = "Value",
                    TheFirstPage = new TheFirstPage
                    {
                        Title = "Title",
                        Description = "Description"
                    },
                    TheSecondPage = new TheSecondPage
                    {
                        Title = "Title {0}",
                    }
                });
            });
            return services;
        }
    }
}
