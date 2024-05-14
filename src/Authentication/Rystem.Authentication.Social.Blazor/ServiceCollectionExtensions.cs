using System.Linq.Dynamic.Core.Tokenizer;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

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
        public long ExpiresIn { get; set; }
        [JsonPropertyName("exping")]
        public DateTime Expiring { get; set; }
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
        public string GetRedirectUri()
            => _navigationManager.BaseUri.Trim('/');
        public async ValueTask<bool> FetchTokenAsync()
        {
            var uri = new Uri(_navigationManager.Uri);
            var queryStrings = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var code = string.Empty;
            var state = string.Empty;
            var localState = await _localStorage.GetStateAsync();
            if (localState != null)
            {
                if (queryStrings.AllKeys.Any(x => x == "state"))
                {
                    state = queryStrings["state"];
                }
                if (!string.IsNullOrWhiteSpace(state))
                {
                    if (state != localState?.Value)
                    {
                        return false;
                    }
                }
                if (queryStrings.AllKeys.Any(x => x == "code"))
                {
                    code = queryStrings["code"];
                }
                if (string.IsNullOrWhiteSpace(code))
                    return false;
                try
                {
                    var token = await _client.GetFromJsonAsync<Token>($"/api/Authentication/Social/Token?provider={localState.Provider}&code={code}");
                    if (token is not null)
                    {
                        await _localStorage.SetTokenAsync(token);
                        _navigationManager.NavigateTo(localState.Path);
                        return true;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }
    }
    public sealed class State
    {
        public required SocialLoginProvider Provider { get; set; }
        public required string Value { get; set; }
        public required DateTime ExpiringTime { get; set; }
        public required string Path { get; set; }
    }
    public sealed class SocialLoginLocalStorageService
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly NavigationManager _navigationManager;

        public SocialLoginLocalStorageService(IJSRuntime jSRuntime, NavigationManager navigationManager)
        {
            _jSRuntime = jSRuntime;
            _navigationManager = navigationManager;
        }
        private const string TokenKey = nameof(TokenKey);
        private const string StateForToken = nameof(StateForToken);
        private const string LocalStorageGetterName = "localStorageFunctions.getItem";
        private const string LocalStorageSetterName = "localStorageFunctions.setItem";
        public async ValueTask<Token?> GetTokenAsync()
        {
            var token = await _jSRuntime.InvokeAsync<string?>(LocalStorageGetterName, TokenKey);
            if (token != null)
            {
                try
                {
                    var tokenFromJson = token.FromJson<Token>();
                    if (tokenFromJson.Expiring > DateTime.UtcNow)
                        return tokenFromJson;
                }
                catch { }
            }
            return default;
        }
        public ValueTask SetTokenAsync(Token token)
        {
            token.Expiring = DateTime.UtcNow.AddSeconds(token.ExpiresIn).AddSeconds(-3);
            return _jSRuntime.InvokeVoidAsync(LocalStorageSetterName, TokenKey, token.ToJson());
        }

        public async ValueTask<State?> GetStateAsync()
        {
            var state = await _jSRuntime.InvokeAsync<string?>(LocalStorageGetterName, StateForToken);
            if (state != null)
            {
                try
                {
                    var stateFromJson = state.FromJson<State>();
                    if (stateFromJson.ExpiringTime > DateTime.UtcNow)
                        return stateFromJson;
                }
                catch { }
            }
            return default;
        }
        public async ValueTask<string> SetStateAsync(SocialLoginProvider provider)
        {
            var state = new State
            {
                Provider = provider,
                Value = Guid.NewGuid().ToString(),
                ExpiringTime = DateTime.UtcNow.AddMinutes(5),
                Path = _navigationManager.Uri
            };
            await _jSRuntime.InvokeVoidAsync(LocalStorageSetterName, StateForToken, state.ToJson());
            return state.Value;
        }

        public ValueTask DeleteStateAsync()
            => _jSRuntime.InvokeVoidAsync(LocalStorageSetterName, StateForToken, null);
    }
}
