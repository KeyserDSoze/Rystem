﻿using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social
{
    internal sealed class FacebookTokenChecker : ITokenChecker
    {
        private readonly IHttpClientFactory _clientFactory;
        public FacebookTokenChecker(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }
        public async Task<TokenResponse?> CheckTokenAndGetUsernameAsync(string code, string? redirectDomain = null, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var client = _clientFactory.CreateClient(Constants.FacebookAuthenticationClient);
                var response = await client.GetAsync($"?fields=email,name&access_token={code}");
                if (response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(message?.Email))
                    {
                        return new TokenResponse
                        {
                            Username = message.Email,
                            Claims =
                            [
                                new Claim(ClaimTypes.Name, message.Name),
                                new Claim(ClaimTypes.Email, message.Email),
                                new Claim(ClaimTypes.NameIdentifier, message.Id)
                            ]
                        };
                    }
                }
            }
            return TokenResponse.Empty;
        }

        private sealed class AuthenticationResponse
        {
            [JsonPropertyName("email")]
            public required string Email { get; set; }
            [JsonPropertyName("name")]
            public required string Name { get; set; }
            [JsonPropertyName("id")]
            public required string Id { get; set; }
        }
    }
}
