using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social
{
    internal sealed class GithubTokenChecker : ITokenChecker
    {
        private const string GithubGetMessage = "client_id={0}&client_secret={1}&code={2}&accept=json";
        private static readonly MediaTypeHeaderValue s_mediaTypeHeaderValue = new("application/x-www-form-urlencoded");
        private const string TokenUri = "login/oauth/access_token";
        private readonly IHttpClientFactory _clientFactory;
        private readonly SocialLoginBuilder _loginBuilder;
        public GithubTokenChecker(IHttpClientFactory clientFactory, SocialLoginBuilder loginBuilder)
        {
            _clientFactory = clientFactory;
            _loginBuilder = loginBuilder;
        }
        private const string Bearer = nameof(Bearer);
        public async Task<TokenResponse?> CheckTokenAndGetUsernameAsync(string code, CancellationToken cancellationToken)
        {
            var settings = _loginBuilder.GitHub;
            var client = _clientFactory.CreateClient(Constants.GitHubAuthenticationClient);
            var content = new StringContent(string.Format(GithubGetMessage, settings.ClientId, settings.ClientSecret, code), s_mediaTypeHeaderValue);
            var response = await client.PostAsync(TokenUri, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                if (message != null)
                {
                    var authResponse = message.FromJson<AuthenticationResponse>();
                    client = _clientFactory.CreateClient(Constants.GitHubAuthenticationClientUser);
                    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, string.Empty);
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue(Bearer, authResponse.AccessToken);
                    var responseFromUser = await client.SendAsync(requestMessage, cancellationToken);
                    if (responseFromUser.IsSuccessStatusCode)
                    {
                        message = await responseFromUser.Content.ReadAsStringAsync();
                        var emails = message.FromJson<SingleEmail[]>();
                        var email = emails.FirstOrDefault(x => x.Primary && x.Verified);
                        if (email != null)
                        {
                            return new TokenResponse
                            {
                                Username = email.Email,
                                Claims =
                                [
                                    new Claim(ClaimTypes.Email, email.Email),
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

        private sealed class SingleEmail
        {
            [JsonPropertyName("email")]
            public required string Email { get; set; }
            [JsonPropertyName("primary")]
            public bool Primary { get; set; }
            [JsonPropertyName("verified")]
            public bool Verified { get; set; }
            [JsonPropertyName("visibility")]
            public required string Visibility { get; set; }
        }
    }
}
