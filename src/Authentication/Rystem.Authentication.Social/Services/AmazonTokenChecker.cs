﻿using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social
{
    internal sealed class AmazonTokenChecker : ITokenChecker
    {
        private readonly IHttpClientFactory _clientFactory;
        public AmazonTokenChecker(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }
        public async Task<AnyOf<TokenResponse?, string>> CheckTokenAndGetUsernameAsync(string code, string? domain = null, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var client = _clientFactory.CreateClient(Constants.AmazonAuthenticationClient);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {code}");
                var response = await client.GetAsync(string.Empty, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
                    if (!string.IsNullOrWhiteSpace(message?.Email))
                    {
                        return new TokenResponse
                        {
                            Username = message.Email,
                            Claims =
                            [
                                new Claim(ClaimTypes.Name, message.Name),
                                new Claim(ClaimTypes.Email, message.Email),
                                new Claim(ClaimTypes.NameIdentifier, message.UserId)
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
            [JsonPropertyName("user_id")]
            public required string UserId { get; set; }
            [JsonPropertyName("name")]
            public required string Name { get; set; }
            [JsonPropertyName("email")]
            public required string Email { get; set; }
        }
    }
}
