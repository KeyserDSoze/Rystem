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
            if (token != null)
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
