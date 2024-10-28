using Google.Apis.Auth.OAuth2;

namespace Rystem.Authentication.Social
{
    public class SocialLoginWithSecretsAndRedirectSettings : SocialLoginWithSecretsSettings
    {
        public List<string>? RedirectDomains { get; set; }
        public override bool IsActive => ClientId != null && ClientSecret != null && RedirectDomains != null && RedirectDomains.Count > 0;
        public string? CheckDomain(string? domain)
        {
            if (domain == null)
            {
                return RedirectDomains?.FirstOrDefault();
            }
            else if (RedirectDomains?.Contains(domain) == true)
            {
                return domain;
            }
            return default;
        }
    }
}
