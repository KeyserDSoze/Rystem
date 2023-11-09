using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Rystem.Authentication.Social
{
    internal sealed class FacebookTokenChecker : ITokenChecker
    {
        public async Task<string> CheckTokenAndGetUsernameAsync(IHttpClientFactory clientFactory, SocialLoginBuilder loginBuilder, string code, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var client = clientFactory.CreateClient(Constants.FacebookAuthenticationClient);
                var response = await client.GetAsync($"?fields=email,name&access_token={code}");
                if (response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(message?.Email))
                    {
                        return message.Email;
                    }
                }
            }
            return string.Empty;
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
