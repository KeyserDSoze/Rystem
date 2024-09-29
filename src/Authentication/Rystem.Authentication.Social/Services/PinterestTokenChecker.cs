using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social
{
    internal sealed class PinterestTokenChecker : ITokenChecker
    {
        private const string PostMessage = "code={0}&redirect_uri={1}/account/login&grant_type=authorization_code";
        private static readonly MediaTypeHeaderValue s_mediaTypeHeaderValue = new("application/x-www-form-urlencoded");
        private const string TokenUri = "/oauth/token";
        private const string MeUri = "/user_account";
        private readonly IHttpClientFactory _clientFactory;
        private readonly SocialLoginBuilder _loginBuilder;
        public PinterestTokenChecker(IHttpClientFactory clientFactory, SocialLoginBuilder loginBuilder)
        {
            _clientFactory = clientFactory;
            _loginBuilder = loginBuilder;
        }
        private const string Bearer = nameof(Bearer);
        private const string Basic = nameof(Basic);
        private static string Btoa(string toEncode)
        {
            var bytes = Encoding.GetEncoding(28591).GetBytes(toEncode);
            var toReturn = Convert.ToBase64String(bytes);
            return toReturn;
        }
        public async Task<TokenResponse?> CheckTokenAndGetUsernameAsync(string code, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var settings = _loginBuilder.Pinterest;
                var client = _clientFactory.CreateClient(Constants.PinterestAuthenticationClient);
                var content = new StringContent(string.Format(PostMessage, code, settings.RedirectDomain), s_mediaTypeHeaderValue);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Basic, Btoa($"{settings.ClientId}:{settings.ClientSecret}"));
                var response = await client.PostAsync(TokenUri, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync();
                    if (message != null)
                    {
                        var authResponse = message.FromJson<AuthenticationResponse>();
                        client = _clientFactory.CreateClient(Constants.PinterestAuthenticationClient);
                        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, MeUri);
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(Bearer, authResponse.AccessToken);
                        var responseFromUser = await client.SendAsync(requestMessage, cancellationToken);
                        if (responseFromUser.IsSuccessStatusCode)
                        {
                            message = await responseFromUser.Content.ReadAsStringAsync();
                            var data = message.FromJson<UserInformation>();
                            return new TokenResponse
                            {
                                Username = data.Email,
                                Claims =
                                [
                                    new Claim(ClaimTypes.Email, data.Email),
                                    new Claim(ClaimTypes.Name, data.Name),
                                    new Claim(ClaimTypes.NameIdentifier, data.Id),
                                ]
                            };
                        }
                    }
                }
            }
            return TokenResponse.Empty;
        }
        private sealed class AuthenticationResponse
        {
            [JsonPropertyName("access_token")]
            public required string AccessToken { get; set; }
            [JsonPropertyName("token_type")]
            public required string TokenType { get; set; }
            [JsonPropertyName("scope")]
            public required string Scope { get; set; }
        }
        private sealed class UserInformation
        {
            [JsonPropertyName("id")]
            public required string Id { get; set; }
            [JsonPropertyName("name")]
            public required string Name { get; set; }
            [JsonPropertyName("email")]
            public required string Email { get; set; }
        }
    }
}
