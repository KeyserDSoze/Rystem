using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social
{
    internal sealed class LinkedinTokenChecker : ITokenChecker
    {
        private const string PostMessage = "client_id={0}&client_secret={1}&grant_type=authorization_code&code={2}&redirect_uri={3}/account/login";
        private static readonly MediaTypeHeaderValue s_mediaTypeHeaderValue = new("application/x-www-form-urlencoded");
        private readonly IHttpClientFactory _clientFactory;
        private readonly SocialLoginBuilder _loginBuilder;

        public LinkedinTokenChecker(IHttpClientFactory clientFactory, SocialLoginBuilder loginBuilder)
        {
            _clientFactory = clientFactory;
            _loginBuilder = loginBuilder;
        }
        private const string Bearer = nameof(Bearer);
        public async Task<AnyOf<TokenResponse?, string>> CheckTokenAndGetUsernameAsync(string code, TokenCheckerSettings settings, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var linkedinSettings = _loginBuilder.Linkedin;
                var domain = linkedinSettings.CheckDomain(settings.Domain);
                if (domain != null)
                {
                    var client = _clientFactory.CreateClient(Constants.LinkedinAuthenticationClient);
                    var content = new StringContent(string.Format(PostMessage, linkedinSettings.ClientId, linkedinSettings.ClientSecret, code, domain), s_mediaTypeHeaderValue);
                    var response = await client.PostAsync(string.Empty, content, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var message = await response.Content.ReadAsStringAsync();
                        if (message != null)
                        {
                            var authResponse = message.FromJson<AuthenticationResponse>();
                            client = _clientFactory.CreateClient(Constants.LinkedinAuthenticationClientUser);
                            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, string.Empty);
                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(Bearer, authResponse.AccessToken);
                            var responseFromUser = await client.SendAsync(requestMessage, cancellationToken);
                            if (responseFromUser.IsSuccessStatusCode)
                            {
                                message = await responseFromUser.Content.ReadAsStringAsync();
                                var profile = message.FromJson<ProfileResponse>();
                                if (profile.EmailVerified)
                                {
                                    return new TokenResponse
                                    {
                                        Username = profile.Email,
                                        Claims =
                                        [
                                            new Claim(ClaimTypes.Email, profile.Email),
                                        new Claim(ClaimTypes.Name, profile.Name),
                                        new Claim(ClaimTypes.GivenName, profile.GivenName),
                                        new Claim(ClaimTypes.Locality, profile.Locale.Language),
                                        new Claim(ClaimTypes.NameIdentifier, profile.Sub),
                                        new Claim(ClaimTypes.Thumbprint, profile.Picture),
                                    ]
                                    };
                                }
                            }
                        }
                    }
                    else
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                        return errorMessage;
                    }
                }
            }
            return TokenResponse.Empty;
        }

        private sealed class AuthenticationResponse
        {
            [JsonPropertyName("access_token")]
            public required string AccessToken { get; set; }
        }

        private sealed class ProfileResponse
        {
            [JsonPropertyName("sub")]
            public required string Sub { get; set; }
            [JsonPropertyName("email_verified")]
            public required bool EmailVerified { get; set; }
            [JsonPropertyName("name")]
            public required string Name { get; set; }
            [JsonPropertyName("locale")]
            public required Locale Locale { get; set; }
            [JsonPropertyName("given_name")]
            public required string GivenName { get; set; }
            [JsonPropertyName("family_name")]
            public required string FamilyName { get; set; }
            [JsonPropertyName("email")]
            public required string Email { get; set; }
            [JsonPropertyName("picture")]
            public required string Picture { get; set; }
        }
        private sealed class Locale
        {
            [JsonPropertyName("country")]
            public required string Country { get; set; }
            [JsonPropertyName("language")]
            public required string Language { get; set; }
        }
    }
}
