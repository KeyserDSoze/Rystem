using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Rystem.Authentication.Social.Blazor
{
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
            if (token != null && token != NullString)
            {
                try
                {
                    var tokenFromJson = token.FromJson<Token>();
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
        private const string NullString = "null";
        public async ValueTask<State?> GetStateAsync()
        {
            var state = await _jSRuntime.InvokeAsync<string?>(LocalStorageGetterName, StateForToken);
            if (state != null && state != NullString)
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

        public ValueTask DeleteTokenAsync()
            => _jSRuntime.InvokeVoidAsync(LocalStorageSetterName, TokenKey, null);
        
        public async ValueTask<T?> GetItemAsync<T>(string key)
        {
            var value = await _jSRuntime.InvokeAsync<string?>(LocalStorageGetterName, key);
            if (value != null && value != NullString)
            {
                try
                {
                    return value.FromJson<T>();
                }
                catch { }
            }
            return default;
        }
        
        public ValueTask SetItemAsync<T>(string key, T value)
            => _jSRuntime.InvokeVoidAsync(LocalStorageSetterName, key, value?.ToJson());
        
        public ValueTask DeleteItemAsync(string key)
            => _jSRuntime.InvokeVoidAsync(LocalStorageSetterName, key, null);
        
        public async ValueTask SetLanguageAsync<TUser>(TUser user)
            where TUser : ILocalizedSocialUser
        {
            if (user?.Language != null)
            {
                var cookieLanguage = await _jSRuntime.InvokeAsync<string>("languageManager.get");
                if (cookieLanguage != user.Language)
                {
                    await _jSRuntime.InvokeVoidAsync("languageManager.set", user.Language);
                }
                CultureInfo.CurrentCulture = new CultureInfo(user.Language);
                CultureInfo.CurrentUICulture = new CultureInfo(user.Language);
            }
            else
            {
                var browserLanguage = await _jSRuntime.InvokeAsync<string>("languageManager.getBrowserLanguage");
                var language = browserLanguage.Split('-').First();
                await _jSRuntime.InvokeVoidAsync("languageManager.set", language);
            }
        }
    }
}
