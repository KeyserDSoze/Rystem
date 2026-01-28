using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Rystem.Authentication.Social
{
    internal sealed class MicrosoftTokenChecker : ITokenChecker
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly SocialLoginBuilder _loginBuilder;
        private static readonly MediaTypeHeaderValue s_mediaTypeHeaderValue = new("application/x-www-form-urlencoded");

        // Default hardcoded code_verifier for backward compatibility
        private const string DefaultCodeVerifier = "19cfc47c216dacba8ca23eeee817603e2ba34fe0976378662ba31688ed302fa9";
        private const string CodeVerifierKey = "code_verifier";

        public MicrosoftTokenChecker(IHttpClientFactory clientFactory, SocialLoginBuilder loginBuilder)
        {
            _clientFactory = clientFactory;
            _loginBuilder = loginBuilder;
        }

        private const string Bearer = nameof(Bearer);

        private static async Task<TokenValidationParameters> GetTokenValidationParametersAsync(string issuer)
        {
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>($"{issuer}/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
            var openIdConfig = await configurationManager.GetConfigurationAsync();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidateSignatureLast = true,
                IssuerSigningKeys = openIdConfig.SigningKeys
            };
            return validationParameters;
        }

        private const string Email = "email";
        private const string PreferredUser = "preferred_username";

        public async Task<AnyOf<TokenResponse?, string>> CheckTokenAndGetUsernameAsync(
            string code,
            TokenCheckerSettings settings,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
                return TokenResponse.Empty;

            var loginSettings = _loginBuilder.Microsoft;
            var domain = loginSettings.CheckDomain(settings.Domain);

            if (domain == null)
                return "Invalid domain";

            // Get code_verifier from settings or use default
            var codeVerifier = settings.GetParameter(CodeVerifierKey) ?? DefaultCodeVerifier;
            var redirectUri = settings.GetRedirectUri();

            var client = _clientFactory.CreateClient(Constants.MicrosoftAuthenticationClient);
            client.DefaultRequestHeaders.Add("Origin", domain);

            // Build token exchange request
            var postData = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", loginSettings.ClientId! },
                { "redirect_uri", redirectUri },
                { "code_verifier", codeVerifier },
                { "grant_type", "authorization_code" },
                { "scope", "profile openid email" }
            };

            var content = new FormUrlEncodedContent(postData);
            var response = await client.PostAsync(string.Empty, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                return $"Token exchange failed: {errorMessage}";
            }

            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(message))
                return "Empty response from provider";

            var authResponse = message.FromJson<AuthenticationResponse>();
            if (authResponse?.AccessToken == null || authResponse.IdToken == null)
                return "Invalid token response";

            try
            {
                var token = new JwtSecurityToken(authResponse.IdToken);
                var issuer = token.Payload.Iss;
                var validationParameters = await GetTokenValidationParametersAsync(issuer);

                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(authResponse.IdToken, validationParameters, out SecurityToken validatedToken);

                var jwt = (JwtSecurityToken)validatedToken;
                var preferredUser = jwt.Claims.FirstOrDefault(x => x.Type == PreferredUser);
                var username = preferredUser != null
                    ? preferredUser.Value
                    : jwt.Claims.First(x => x.Type == Email).Value;

                return new TokenResponse
                {
                    Username = username,
                    Claims = [.. jwt.Claims]
                };
            }
            catch (SecurityTokenValidationException ex)
            {
                return $"Token validation failed: {ex.Message}";
            }
        }

        private sealed class AuthenticationResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("id_token")]
            public string? IdToken { get; set; }
        }
    }
}
