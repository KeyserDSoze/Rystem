using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Rystem.Authentication.Social
{
    internal sealed class MicrosoftTokenChecker : ITokenChecker
    {
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
        public async Task<string> CheckTokenAndGetUsernameAsync(IHttpClientFactory clientFactory, SocialLoginBuilder loginBuilder, string code, CancellationToken cancellationToken)
        {
            var token = new JwtSecurityToken(code);
            var issuer = token.Payload.Iss;
            var validationParameters = await GetTokenValidationParametersAsync(issuer);
            var counter = 0;
            while (counter <= 1)
            {
                counter++;
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    tokenHandler.ValidateToken(code, validationParameters, out SecurityToken validatedToken);
                    var jwt = (JwtSecurityToken)validatedToken;
                    return jwt.Claims.First(x => x.Type == Email).Value;
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
            return string.Empty;
        }
    }
}
