using System.Net.Http.Json;
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
        public async Task<string> CheckTokenAndGetUsernameAsync(string code, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var client = _clientFactory.CreateClient(Constants.AmazonAuthenticationClient);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {code}");
                var response = await client.GetAsync(string.Empty);
                if (response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
                    if (!string.IsNullOrWhiteSpace(message?.Email))
                    {
                        return message.Email;
                    }
                }
            }
            return string.Empty;
        }

        public class AuthenticationResponse
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
