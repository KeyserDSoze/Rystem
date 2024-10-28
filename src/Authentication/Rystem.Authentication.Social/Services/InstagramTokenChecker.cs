using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social
{
    internal sealed class InstagreamTokenChecker : ITokenChecker
    {
        private const string PostMessage = "client_id={0}&client_secret={1}&grant_type=authorization_code&code={2}&redirect_uri={3}/account/login";
        private static readonly MediaTypeHeaderValue s_mediaTypeHeaderValue = new("application/x-www-form-urlencoded");
        private readonly IHttpClientFactory _clientFactory;
        private readonly SocialLoginBuilder _loginBuilder;
        public InstagreamTokenChecker(IHttpClientFactory clientFactory, SocialLoginBuilder loginBuilder)
        {
            _clientFactory = clientFactory;
            _loginBuilder = loginBuilder;
        }
        private const string Bearer = nameof(Bearer);
        private const string Basic = nameof(Basic);
        public async Task<TokenResponse?> CheckTokenAndGetUsernameAsync(string code, string? domain = null, CancellationToken cancellationToken = default)
        {
            var settings = _loginBuilder.Instagram;
            domain = settings.CheckDomain(domain);
            if (domain != null)
            {
                var client = _clientFactory.CreateClient(Constants.InstagramAuthenticationClient);
                var content = new StringContent(string.Format(PostMessage, settings.ClientId, settings.ClientSecret, code, domain), s_mediaTypeHeaderValue);
                var response = await client.PostAsync(string.Empty, content, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync();
                    if (message != null)
                    {
                        var authResponse = message.FromJson<AuthenticationResponse>();
                        client = _clientFactory.CreateClient(Constants.InstagramAuthenticationClientUser);
                        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"access_token={authResponse.AccessToken}");
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
