using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social
{
    internal sealed class XTokenChecker : ITokenChecker
    {
        private const string PostMessage = "client_id={0}&redirect_uri={1}/account/login&code={2}&grant_type=authorization_code&code_verifier=challenge";
        private static readonly MediaTypeHeaderValue s_mediaTypeHeaderValue = new("application/x-www-form-urlencoded");
        private const string TokenUri = "oauth2/token";
        private const string MeUri = "users/me";
        private readonly IHttpClientFactory _clientFactory;
        private readonly SocialLoginBuilder _loginBuilder;
        public XTokenChecker(IHttpClientFactory clientFactory, SocialLoginBuilder loginBuilder)
        {
            _clientFactory = clientFactory;
            _loginBuilder = loginBuilder;
        }
        private const string Bearer = nameof(Bearer);
        private const string Basic = nameof(Basic);
        public async Task<TokenResponse?> CheckTokenAndGetUsernameAsync(string code, string? domain = null, CancellationToken cancellationToken = default)
        {
            var settings = _loginBuilder.X;
            domain = settings.CheckDomain(domain);
            if (domain != null)
            {
                var client = _clientFactory.CreateClient(Constants.XAuthenticationClient);
                var content = new StringContent(string.Format(PostMessage, settings.ClientId, domain, code), s_mediaTypeHeaderValue);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Basic, $"{settings.ClientId}:{settings.ClientSecret}".ToBase64());
                var response = await client.PostAsync(TokenUri, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync();
                    if (message != null)
                    {
                        var authResponse = message.FromJson<AuthenticationResponse>();
                        client = _clientFactory.CreateClient(Constants.XAuthenticationClient);
                        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, MeUri);
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(Bearer, authResponse.AccessToken);
                        var responseFromUser = await client.SendAsync(requestMessage, cancellationToken);
                        if (responseFromUser.IsSuccessStatusCode)
                        {
                            message = await responseFromUser.Content.ReadAsStringAsync();
                            var data = message.FromJson<DataResponse>();
                            return new TokenResponse
                            {
                                Username = data.Data.Username,
                                Claims =
                                [
                                    new Claim(ClaimTypes.Name, data.Data.Name),
                                new Claim(ClaimTypes.NameIdentifier, data.Data.Id),
                                new Claim(ClaimTypes.Email, data.Data.Username),
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
        private sealed class DataResponse
        {
            [JsonPropertyName("data")]
            public required UserInformation Data { get; set; }
        }
        private sealed class UserInformation
        {
            [JsonPropertyName("id")]
            public required string Id { get; set; }
            [JsonPropertyName("name")]
            public required string Name { get; set; }
            [JsonPropertyName("username")]
            public required string Username { get; set; }
        }
    }
}
