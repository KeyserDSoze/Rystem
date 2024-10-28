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
        private const string PostMessage = "code={0}&scope=profile openid email&client_id={1}&redirect_uri={2}&code_verifier=19cfc47c216dacba8ca23eeee817603e2ba34fe0976378662ba31688ed302fa9&grant_type=authorization_code";
        public MicrosoftTokenChecker(IHttpClientFactory clientFactory, SocialLoginBuilder loginBuilder)
        {
            _clientFactory = clientFactory;
            _loginBuilder = loginBuilder;
        }
        private const string Bearer = nameof(Bearer);
        private static TokenValidationParameters? s_parameters;
        private static DateTime? s_nextUpdate;
        private static Task<TokenValidationParameters> GetTokenValidationParametersAsync(string issuer)
        {
            if (s_parameters == null)
                return RefreshTokenValidationParametersAsync(issuer);
            return Task.FromResult(s_parameters);
        }
        private static async Task<TokenValidationParameters> RefreshTokenValidationParametersAsync(string issuer)
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
            s_parameters = validationParameters;
            s_nextUpdate = DateTime.UtcNow.AddHours(1);
            return validationParameters;
        }
        private const string Email = "email";
        private const string PreferredUser = "preferred_username";
        public async Task<TokenResponse?> CheckTokenAndGetUsernameAsync(string code, string? domain = null, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var settings = _loginBuilder.Microsoft;
                domain = settings.CheckDomain(domain);
                if (domain != null)
                {
                    var client = _clientFactory.CreateClient(Constants.MicrosoftAuthenticationClient);
                    client.DefaultRequestHeaders.Add("Origin", domain);
                    var content = new StringContent(string.Format(PostMessage, code, settings.ClientId, $"{domain.Trim('/')}/"), s_mediaTypeHeaderValue);
                    var response = await client.PostAsync(string.Empty, content, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var message = await response.Content.ReadAsStringAsync();
                        if (message != null)
                        {
                            var authResponse = message.FromJson<AuthenticationResponse>();
                            if (authResponse.AccessToken != null)
                            {
                                var token = new JwtSecurityToken(authResponse.IdToken);
                                var issuer = token.Payload.Iss;
                                var validationParameters = await GetTokenValidationParametersAsync(issuer);
                                var counter = 0;
                                while (counter <= 1)
                                {
                                    counter++;
                                    try
                                    {
                                        var tokenHandler = new JwtSecurityTokenHandler();
                                        tokenHandler.ValidateToken(authResponse.IdToken, validationParameters, out SecurityToken validatedToken);
                                        var jwt = (JwtSecurityToken)validatedToken;
                                        var preferredUser = jwt.Claims.FirstOrDefault(x => x.Type == PreferredUser);
                                        var username = preferredUser != null ? preferredUser.Value : jwt.Claims.First(x => x.Type == Email).Value;
                                        return new TokenResponse
                                        {
                                            Username = username,
                                            Claims = jwt.Claims.ToList()
                                        };
                                    }
                                    catch (SecurityTokenValidationException)
                                    {
                                        if (s_nextUpdate > DateTime.UtcNow)
                                        {
                                            validationParameters = await RefreshTokenValidationParametersAsync(issuer);
                                        }
                                        else
                                        {
                                            counter++;
                                        }
                                    }
                                }
                            }
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
            [JsonPropertyName("id_token")]
            public required string IdToken { get; set; }
        }
    }
}
