using Google.Apis.Auth.OAuth2;

namespace Rystem.Authentication.Social
{
    public class SocialLoginWithSecretsAndRedirectSettings : SocialLoginWithSecretsSettings
    {
        public string? RedirectDomain { get; set; }
        public override bool IsActive => ClientId != null && ClientSecret != null && RedirectDomain != null;
    }
}
