using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Google.Apis.Auth;

namespace Rystem.Authentication.Social
{
    internal sealed class GoogleTokenChecker : ITokenChecker
    {
        private const string PostMessage = "client_id={0}&client_secret={1}&grant_type=authorization_code&code={2}&redirect_uri={3}";
        private static readonly MediaTypeHeaderValue s_mediaTypeHeaderValue = new("application/x-www-form-urlencoded");
        private readonly IHttpClientFactory _clientFactory;
        private readonly SocialLoginBuilder _loginBuilder;
        public GoogleTokenChecker(IHttpClientFactory clientFactory, SocialLoginBuilder loginBuilder)
        {
            _clientFactory = clientFactory;
            _loginBuilder = loginBuilder;
        }
        public async Task<AnyOf<TokenResponse?, string>> CheckTokenAndGetUsernameAsync(string code, string? domain = null, CancellationToken cancellationToken = default)
        {
            var settings = _loginBuilder.Google;
            domain = settings.CheckDomain(domain);
            if (domain != null)
            {
                var client = _clientFactory.CreateClient(Constants.GoogleAuthenticationClient);
                var content = new StringContent(string.Format(PostMessage, settings.ClientId, settings.ClientSecret, code, domain), s_mediaTypeHeaderValue);
                var response = await client.PostAsync(string.Empty, content);
                if (response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(cancellationToken);
                    if (message != null)
                    {
                        var payload = await GoogleJsonWebSignature.ValidateAsync(message.IdToken);
                        return new TokenResponse
                        {
                            Username = payload.Email,
                            Claims =
                            [
                                new Claim(ClaimTypes.Email, payload.Email),
                                new Claim(ClaimTypes.Name, payload.Name),
                                new Claim(ClaimTypes.GivenName, payload.FamilyName),
                                new Claim(ClaimTypes.Locality, payload.Locale),
                                new Claim(ClaimTypes.NameIdentifier, payload.Subject),
                                new Claim(ClaimTypes.Thumbprint, payload.Picture),
                            ]
                        };
                    }
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                    return errorMessage;
                }
            }
            return TokenResponse.Empty;
        }
        private sealed class AuthenticationResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }
            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }
            [JsonPropertyName("scope")]
            public string? Scope { get; set; }
            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }
            [JsonPropertyName("id_token")]
            public string? IdToken { get; set; }
        }
    }
}
