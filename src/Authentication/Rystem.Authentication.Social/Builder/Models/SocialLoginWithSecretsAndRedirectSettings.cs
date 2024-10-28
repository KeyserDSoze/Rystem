using Google.Apis.Auth.OAuth2;

namespace Rystem.Authentication.Social
{
    public class SocialLoginWithSecretsAndRedirectSettings : SocialLoginWithSecretsSettings
    {
        public List<string>? AllowedDomains { get; set; }
        public override bool IsActive => ClientId != null && ClientSecret != null && AllowedDomains != null && AllowedDomains.Count > 0;
        public string? CheckDomain(string? domain)
        {
            if (domain == null)
            {
                return AllowedDomains?.FirstOrDefault();
            }
            else if (AllowedDomains?.Contains(domain) == true)
            {
                return domain;
            }
            return default;
        }
    }
}
