using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.JSInterop;
using Rystem.Authentication.Social.Blazor.Components;

namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class SocialLoginAppSettings
    {
        public string? ApiUrl { get; set; }
        public SocialParameter Google { get; set; } = new();
        public SocialParameter Facebook { get; set; } = new();
        public SocialParameter Microsoft { get; set; } = new();
    }
    public sealed class SocialParameter
    {
        public string? ClientId { get; set; }
    }
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSocialLoginUI(this IServiceCollection services,
            Action<SocialLoginAppSettings> settings)
        {
            var options = new SocialLoginAppSettings()
            {
            };
            settings.Invoke(options);
            services.AddSingleton(options);
            services.AddScoped<SocialLoginLocalStorageService>();
            services.AddHttpClient(nameof(SocialLoginManager), x =>
            {
                x.BaseAddress = new Uri(options.ApiUrl!);
            });
            services.AddTransient<SocialLoginManager>();
            return services;
        }
    }
    public enum SocialLoginProvider
    {
        DotNet,
        Google,
        Microsoft,
        Facebook,
        GitHub,
        Amazon,
        Linkedin,
        X,
        Instagram,
        Pinterest,
        TikTok
    }
    public sealed class Token
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; }
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; }
        [JsonPropertyName("isExpired")]
        public bool IsExpired { get; set; }
        [JsonPropertyName("expiresIn")]
        public DateTime ExpiresIn { get; set; }
    }
    public sealed class SocialLoginManager
    {
        private readonly SocialLoginAppSettings _settings;
        private readonly SocialLoginLocalStorageService _localStorage;
        private readonly NavigationManager _navigationManager;
        private readonly HttpClient _client;
        public SocialLoginManager(SocialLoginAppSettings settings,
            SocialLoginLocalStorageService localStorage,
            IHttpClientFactory httpClientFactory,
            NavigationManager navigationManager)
        {
            _settings = settings;
            _localStorage = localStorage;
            _navigationManager = navigationManager;
            _client = httpClientFactory.CreateClient(nameof(SocialLoginManager));
        }
        public ValueTask<bool> FetchTokenAsync(string provider)
            => FetchTokenAsync(Enum.Parse<SocialLoginProvider>(provider, true));
        public async ValueTask<bool> FetchTokenAsync(SocialLoginProvider provider)
        {
            var uri = new Uri(_navigationManager.Uri);
            var queryStrings = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var code = string.Empty;
            var state = string.Empty;
            if (queryStrings.AllKeys.Any(x => x == "state"))
            {
                state = queryStrings["state"];
            }
            if (!string.IsNullOrWhiteSpace(state))
            {
                if (state != await _localStorage.GetStateAsync())
                {
                    return false;
                }
            }
            if (queryStrings.AllKeys.Any(x => x == "code"))
            {
                code = queryStrings["code"];
            }
            try
            {
                var token = await _client.GetFromJsonAsync<Token>($"/api/Authentication/Social/Token?provider=${(int)provider}&code=${code}");
                if (token is not null)
                {
                    await _localStorage.SetTokenAsync(token.ToJson());
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }
    }
    public sealed class SocialLoginLocalStorageService
    {
        private readonly IJSRuntime _jSRuntime;
        public SocialLoginLocalStorageService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }
        private const string TokenKey = nameof(TokenKey);
        private const string StateForToken = nameof(StateForToken);
        private const string LocalStorageGetterName = "localStorageFunctions.getItem";
        private const string LocalStorageSetterName = "localStorageFunctions.setItem";
        public ValueTask<string?> GetTokenAsync()
            => _jSRuntime.InvokeAsync<string?>(LocalStorageGetterName, TokenKey);
        public ValueTask SetTokenAsync(string token)
            => _jSRuntime.InvokeVoidAsync(LocalStorageSetterName, TokenKey, token);
        public ValueTask<string?> GetStateAsync()
            => _jSRuntime.InvokeAsync<string?>(LocalStorageGetterName, StateForToken);
        public ValueTask SetStateAsync(string state)
            => _jSRuntime.InvokeVoidAsync(LocalStorageSetterName, StateForToken, state);
    }
    public static class WebApplicationExtensions
    {
        public static T UseSocialLogin<T>(this T builder)
            where T : IEndpointRouteBuilder
        {
            builder.MapRazorComponents<LoginConfirmation>().AddInteractiveServerRenderMode();
            return builder;
        }
    }
}
